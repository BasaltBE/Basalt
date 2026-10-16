using System.Diagnostics;
using System.Threading.Channels;
using BasaltBE.Basalt;
using BasaltBE.Basalt.Commands;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Commands.Vanilla;
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

internal static class Program
{
    private const int TickRate = 20;
    private const int MinimumSleepMilliseconds = 10;
    private const double MinimumTickRate = 19;
    private static readonly ScopedLogger Log = Logger.Scoped("DedicatedServer");

    private static void Main(string[] args)
    {
        #region Startup

        Profiler.Enabled = args.Contains("--tracy", StringComparer.OrdinalIgnoreCase);
        Profiler.SetThreadName("DedicatedServer");
        Logger.MinimumLevel = LogLevel.Info;

        using ManualResetEventSlim shutdown = new(false);
        ConsoleCancelEventHandler cancel = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            Log.Info("Shutdown requested.");
            shutdown.Set();
        };
        Console.CancelKeyPress += cancel;

        #endregion

        try
        {
            #region Server Setup

            const string propertiesPath = "server.properties";
            ServerProperties properties = ServerPropertiesFile.LoadFromPath(propertiesPath);
            ServerPropertiesFile.SaveToPath(propertiesPath, properties);

            EntityPalette.RegisterTrait<PlayerChunkRenderingTrait>();
            EntityPalette.RegisterTrait<PlayerInventoryTrait>();
            EntityPalette.RegisterTrait<EntityMovementTrait>();

            PermissionStore permissions = new();
            using PermissionFile permissionFile = new(permissions, "permissions.json");
            using Server server = new(properties, permissions: permissions);

            #endregion

            #region Commands and Permissions

            server.Commands.Register(new StatusCommand());
            server.Commands.Register(new GiveCommand());
            server.Commands.Register(new ListCommand());
            server.Commands.Register(new OpCommand());
            server.Commands.Register(new DeopCommand());
            server.Commands.Register(new StopCommand());

            string[] operators = args.Where(argument => argument.StartsWith("--operator=", StringComparison.Ordinal))
                .Select(argument => argument[11..]).ToArray();

            server.Events.Subscribe<PlayerJoinEvent>(joined =>
            {
                if (operators.Contains(joined.Player.Username, StringComparer.OrdinalIgnoreCase) ||
                    operators.Contains(joined.Player.Xuid, StringComparer.Ordinal))
                    joined.Player.Permissions.Add("basalt.operator");
            });

            #endregion

            #region Worlds and Generators

            server.Generators.Register<SuperflatGenerator>("superflat");
            server.Generators.Register<VoidGenerator>("void");
            server.Generators.SetDefault("superflat");

            World world = server.Worlds.FirstOrDefault(world =>
                string.Equals(world.Identifier, properties.DefaultWorldName, StringComparison.OrdinalIgnoreCase)
            ) ?? server.CreateWorld(properties.DefaultWorldName, static world =>
            {
                world.CreateDimension("minecraft:overworld");
            });
            _ = world.GetDimension("minecraft:overworld") ?? world.CreateDimension("minecraft:overworld");

            #endregion

            server.Start();
            Log.Info($"NetherNet is listening on network {server.Network.NetworkId}.");

            #region Console Input

            Channel<string> consoleCommands = Channel.CreateBounded<string>(new BoundedChannelOptions(64)
            {
                SingleReader = true,
                SingleWriter = true,
                FullMode = BoundedChannelFullMode.Wait
            });
            ServerCommandExecutor console = new(server);

            Thread input = new(() =>
            {
                try
                {
                    while (Console.ReadLine() is { } command)
                    {
                        if (string.IsNullOrWhiteSpace(command))
                            continue;
                        if (!consoleCommands.Writer.TryWrite(command))
                            Log.Warn("Console command queue is full.");
                    }
                }
                catch (IOException error)
                {
                    Log.Warn($"Console input stopped: {error.Message}");
                }
                finally
                {
                    consoleCommands.Writer.TryComplete();
                }
            })
            {
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
            long slowestTickDuration = 0;
            long maximumTickLateness = 0;

            while (!shutdown.IsSet && !server.Stopping)
            {
                long remaining = nextTick - Stopwatch.GetTimestamp();
                long sleepTicks = remaining - sleepOvershoot;
                if (sleepTicks > 0)
                {
                    long sleepMilliseconds = sleepTicks * 1000 / Stopwatch.Frequency;
                    if (sleepMilliseconds > 0)
                    {
                        long requestedSleepTicks = sleepMilliseconds * Stopwatch.Frequency / 1000;
                        long beforeSleep = Stopwatch.GetTimestamp();
                        if (shutdownWaitHandle.WaitOne((int)sleepMilliseconds))
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
                maximumTickLateness = Math.Max(maximumTickLateness, tickStart - nextTick);

                for (int index = 0; index < 16 && !server.Stopping &&
                    consoleCommands.Reader.TryRead(out string? command); index++)
                {
                    try
                    {
                        CommandResult result = console.Execute(command);
                        if (result.Success)
                            Log.Info(result.Message);
                        else
                            Log.Warn(result.Message);
                    }
                    catch (Exception error)
                    {
                        Log.Error($"Console command failed: {error}");
                    }
                }

                if (server.Stopping)
                    break;

                server.Tick();
                long tickDurationTicks = Stopwatch.GetTimestamp() - tickStart;
                slowestTickDuration = Math.Max(slowestTickDuration, tickDurationTicks);

                if (ticks == 0)
                {
                    reportStart = tickStart;
                    ticks = 1;
                }
                else
                {
                    ticks++;
                    long reportDuration = tickStart - reportStart;
                    if (reportDuration >= Stopwatch.Frequency)
                    {
                        double ticksPerSecond = (ticks - 1) * (double)Stopwatch.Frequency / reportDuration;
                        if (ticksPerSecond < MinimumTickRate)
                            Log.Warn(
                                $"Server tick rate is below 19 TPS: {ticksPerSecond:F2} TPS " +
                                $"({1000 / ticksPerSecond:F2} ms average interval, " +
                                $"{slowestTickDuration * 1000.0 / Stopwatch.Frequency:F2} ms slowest tick, " +
                                $"{maximumTickLateness * 1000.0 / Stopwatch.Frequency:F2} ms max start lateness)."
                            );

                        ticks = 1;
                        slowestTickDuration = 0;
                        maximumTickLateness = 0;
                        reportStart = tickStart;
                    }
                }

                nextTick += tickDuration;
                long now = Stopwatch.GetTimestamp();
                if (nextTick <= now)
                    nextTick += ((now - nextTick) / tickDuration + 1) * tickDuration;
            }

            #endregion
        }
        finally
        {
            Console.CancelKeyPress -= cancel;
        }
    }
}
