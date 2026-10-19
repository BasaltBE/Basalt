using System.Diagnostics;
using BasaltBE.Basalt;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.BlockActors;
using BasaltBE.Basalt.Blocks.Traits;
using BasaltBE.Basalt.Commands;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Commands.Vanilla;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Events.Players;
using BasaltBE.Basalt.Permissions;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Basalt.Properties;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Logging;
using BasaltBE.Logging.Enums;

namespace BasaltBE.DedicatedServer;

internal static class Program {
    private const int TickRate = 20;
    private const int MinimumSleepMilliseconds = 10;
    private const double WarningTickRate = 18;
    private static readonly ScopedLogger Log = Logger.Scoped("DedicatedServer");

    private static void Main(string[] args) {
        #region Startup

        Profiler.Enabled = args.Contains("--tracy", StringComparer.OrdinalIgnoreCase);
        Profiler.SetThreadName("DedicatedServer");
        Logger.MinimumLevel = LogLevel.Info;

        using ManualResetEventSlim shutdown = new(false);
        ConsoleCancelEventHandler cancel = (_, eventArgs) => {
            eventArgs.Cancel = true;
            Log.Info("Shutdown requested.");
            shutdown.Set();
        };
        Console.CancelKeyPress += cancel;

        #endregion

        try {
            #region Server Setup

            const string propertiesPath = "server.properties";
            ServerProperties properties = ServerPropertiesFile.LoadFromPath(propertiesPath);
            ServerPropertiesFile.SaveToPath(propertiesPath, properties);

            EntityPalette.RegisterTrait<PlayerChunkRenderingTrait>();
            EntityPalette.RegisterTrait<PlayerInventoryTrait>();
            EntityPalette.RegisterTrait<EntityMovementTrait>();
            EntityPalette.RegisterTrait<EntityHealthTrait>();
            BlockPalette.RegisterTrait<CraftingTableTrait>();
            BlockPalette.RegisterTrait<FenceTrait>();
            BlockPalette.RegisterTrait<FenceGateTrait>();
            BlockPalette.RegisterTrait<StairShapeTrait>("minecraft:cornerable_stairs");

            PermissionStore permissions = new();
            using PermissionFile permissionFile = new(permissions, "permissions.json");
            using Server server = new(properties, permissions: permissions);
            server.BlockActors.Register("minecraft:chest", static context => new ChestBlockActor(context));

            #endregion

            #region Commands and Permissions

            server.Commands.Register(new StatusCommand());
            server.Commands.Register(new GiveCommand());
            server.Commands.Register(new ListCommand());
            server.Commands.Register(new OpCommand());
            server.Commands.Register(new DeopCommand());
            server.Commands.Register(new StopCommand());
            server.Commands.Register(new SummonCommand());

            string[] operators = args.Where(argument => argument.StartsWith("--operator=", StringComparison.Ordinal))
                .Select(argument => argument[11..]).ToArray();

            server.Events.Subscribe<PlayerJoinEvent>(joined => {
                if (operators.Contains(joined.Player.Username, StringComparer.OrdinalIgnoreCase) ||
                    operators.Contains(joined.Player.Xuid, StringComparer.Ordinal))
                    joined.Player.Permissions.Add("basalt.operator");
            });

            // server.Events.Subscribe<PlayerSpawnEvent>(spawned =>
            // {
            //     Entity zombie = new("minecraft:zombie");
            //     zombie.Position.X = spawned.Player.Position.X;
            //     zombie.Position.Y = spawned.Player.Position.Y + 2;
            //     zombie.Position.Z = spawned.Player.Position.Z;
            //     zombie.Spawn(spawned.Player.Dimension);
            // });

            #endregion

            #region Worlds and Generators

            server.Generators.Register<SuperflatGenerator>("superflat");
            server.Generators.Register<VoidGenerator>("void");
            server.Generators.SetDefault("superflat");

            World world = server.Worlds.FirstOrDefault(world =>
                string.Equals(world.Identifier, properties.DefaultWorldName, StringComparison.OrdinalIgnoreCase)
            ) ?? server.CreateWorld(properties.DefaultWorldName, static world => {
                world.CreateDimension("minecraft:overworld");
            });
            _ = world.GetDimension("minecraft:overworld") ?? world.CreateDimension("minecraft:overworld");

            #endregion

            server.Start();
            Log.Info($"NetherNet is listening on network {server.Network.NetworkId}.");

            #region Console Input

            ServerCommandExecutor console = new(server);
            Action<CommandResult> commandCompleted = result => {
                if (result.Success)
                    Log.Info(result.Message);
                else
                    Log.Warn(result.Message);
            };

            Thread input = new(() => {
                try {
                    while (Console.ReadLine() is { } command) {
                        if (string.IsNullOrWhiteSpace(command))
                            continue;
                        if (!server.Commands.Enqueue(command, console, commandCompleted))
                            Log.Warn("Console command queue is full.");
                    }
                }
                catch (IOException error) {
                    Log.Warn($"Console input stopped: {error.Message}");
                }
            }) {
                IsBackground = true,
                Name = "Console input"
            };
            input.Start();

            #endregion

            #region Tick Loop

            long tickDuration = Stopwatch.Frequency / TickRate;
            long minimumSleepDuration = Stopwatch.Frequency * MinimumSleepMilliseconds / 1000;
            long maximumSleepOvershoot = tickDuration - minimumSleepDuration;
            long nextTick = Stopwatch.GetTimestamp();
            long reportStart = 0;
            long sleepOvershoot = Stopwatch.Frequency / 1000;
            WaitHandle shutdownWaitHandle = shutdown.WaitHandle;
            int ticks = 0;
            TimeSpan lastGcPauseDuration = GC.GetTotalPauseDuration();
            int lastGen0CollectionCount = GC.CollectionCount(0);
            int lastGen1CollectionCount = GC.CollectionCount(1);
            int lastGen2CollectionCount = GC.CollectionCount(2);

            while (!shutdown.IsSet && !server.Stopping) {
                long remaining = nextTick - Stopwatch.GetTimestamp();
                long sleepTicks = remaining - sleepOvershoot;
                if (sleepTicks > 0) {
                    long sleepMilliseconds = sleepTicks * 1000 / Stopwatch.Frequency;
                    if (sleepMilliseconds > 0) {
                        long requestedSleepTicks = sleepMilliseconds * Stopwatch.Frequency / 1000;
                        long beforeSleep = Stopwatch.GetTimestamp();
                        bool shuttingDown = false;
                        long sleepDeadline = beforeSleep + requestedSleepTicks;
                        do {
                            server.Poll();
                            int wait = (int)Math.Clamp((sleepDeadline - Stopwatch.GetTimestamp()) * 1000 / Stopwatch.Frequency, 0, 5);
                            if (shutdownWaitHandle.WaitOne(wait)) {
                                shuttingDown = true;
                                break;
                            }
                        } while (Stopwatch.GetTimestamp() < sleepDeadline);
                        if (shuttingDown)
                            break;

                        long overshoot = Stopwatch.GetTimestamp() - beforeSleep - requestedSleepTicks;
                        sleepOvershoot = Math.Clamp(
                            (sleepOvershoot * 7 + Math.Max(overshoot, 0)) / 8,
                            0,
                            maximumSleepOvershoot
                        );
                    }
                }

                while (!shutdown.IsSet && Stopwatch.GetTimestamp() < nextTick)
                    Thread.SpinWait(1);

                if (shutdown.IsSet)
                    break;

                long tickStart = Stopwatch.GetTimestamp();

                if (server.Stopping)
                    break;

                server.Tick();

                if (ticks == 0) {
                    reportStart = tickStart;
                    ticks = 1;
                }
                else {
                    ticks++;
                    long reportDuration = tickStart - reportStart;
                    if (reportDuration >= Stopwatch.Frequency) {
                        double ticksPerSecond = (ticks - 1) * (double)Stopwatch.Frequency / reportDuration;
                        TimeSpan gcPauseDuration = GC.GetTotalPauseDuration();
                        double gcPauseMilliseconds = Math.Max(
                            (gcPauseDuration - lastGcPauseDuration).TotalMilliseconds,
                            0
                        );
                        lastGcPauseDuration = gcPauseDuration;
                        int gen0Collections = GC.CollectionCount(0);
                        int gen1Collections = GC.CollectionCount(1);
                        int gen2Collections = GC.CollectionCount(2);
                        int gen0CollectionsInSample = gen0Collections - lastGen0CollectionCount;
                        int gen1CollectionsInSample = gen1Collections - lastGen1CollectionCount;
                        int gen2CollectionsInSample = gen2Collections - lastGen2CollectionCount;
                        lastGen0CollectionCount = gen0Collections;
                        lastGen1CollectionCount = gen1Collections;
                        lastGen2CollectionCount = gen2Collections;
                        if (ticksPerSecond < WarningTickRate) {
                            double averageMilliseconds = server.Ticks.AverageMilliseconds;
                            double lastMilliseconds = server.Ticks.LastMilliseconds;
                            double peakMilliseconds = server.Ticks.MaximumMilliseconds;
                            string averageColor = averageMilliseconds >= 40 ? "§c" : averageMilliseconds >= 20 ? "§e" : "§a";
                            string lastColor = lastMilliseconds >= 40 ? "§c" : lastMilliseconds >= 20 ? "§e" : "§a";
                            string peakColor = peakMilliseconds >= 40 ? "§c" : peakMilliseconds >= 20 ? "§e" : "§a";
                            string gcColor = gcPauseMilliseconds >= 40 ? "§c" : gcPauseMilliseconds >= 20 ? "§e" : "§a";
                            Log.Warn(FormattableString.Invariant(
                                $"""
                                §6Server Performance§r
                                §8` §7TPS (§c{ticksPerSecond:F2}§7)  MSPT ({averageColor}{averageMilliseconds:F3}§7)§r
                                §8` §7Tick ({lastColor}{lastMilliseconds:F3}§7 ms last, {peakColor}{peakMilliseconds:F3}§7 ms peak)§r
                                §8` §7GC ({gcColor}{gcPauseMilliseconds:F2}§7 ms pause, Gen0/1/2 {gen0CollectionsInSample}/{gen1CollectionsInSample}/{gen2CollectionsInSample})§r
                                """
                            ));
                        }

                        ticks = 1;
                        reportStart = tickStart;
                    }
                }

                nextTick += tickDuration;
                long now = Stopwatch.GetTimestamp();
                if (nextTick <= now) {
                    nextTick += ((now - nextTick) / tickDuration + 1) * tickDuration;
                }
            }

            #endregion
        }
        finally {
            Console.CancelKeyPress -= cancel;
        }
    }
}
