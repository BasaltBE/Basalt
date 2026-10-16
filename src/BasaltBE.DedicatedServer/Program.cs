using System.Diagnostics;
using BasaltBE.Basalt;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Basalt.Properties;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Logging;
using BasaltBE.Logging.Enums;

namespace BasaltBE.DedicatedServer;

internal static class Program {
    private const int TickRate = 20;
    private const int MinimumSleepMilliseconds = 10;
    private const double MinimumTickRate = 19;
    private static readonly ScopedLogger Log = Logger.Scoped("DedicatedServer");

    private static void Main(string[] args) {
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

        try {
            const string propertiesPath = "server.properties";
            ServerProperties properties = ServerPropertiesFile.LoadFromPath(propertiesPath);
            ServerPropertiesFile.SaveToPath(propertiesPath, properties);

            EntityPalette.RegisterTrait<PlayerChunkRenderingTrait>();
            EntityPalette.RegisterTrait<PlayerInventoryTrait>();
            EntityPalette.RegisterTrait<EntityMovementTrait>();

            using Server server = new(properties);
            server.Generators.Register<SuperflatGenerator>("superflat");
            server.Generators.Register<VoidGenerator>("void");
            server.Generators.SetDefault("superflat");

            World world = server.Worlds.FirstOrDefault(world =>
                string.Equals(world.Identifier, properties.DefaultWorldName, StringComparison.OrdinalIgnoreCase)
            ) ?? server.CreateWorld(properties.DefaultWorldName);
            _ = world.GetDimension("minecraft:overworld") ?? world.CreateDimension("minecraft:overworld");

            server.Start();
            Log.Info($"NetherNet is listening on network {server.Network.NetworkId}.");

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
            while (!shutdown.IsSet) {
                long remaining = nextTick - Stopwatch.GetTimestamp();
                long sleepTicks = remaining - sleepOvershoot;
                if (sleepTicks > 0) {
                    long sleepMilliseconds = sleepTicks * 1000 / Stopwatch.Frequency;
                    if (sleepMilliseconds > 0) {
                        long requestedSleepTicks = sleepMilliseconds * Stopwatch.Frequency / 1000;
                        long beforeSleep = Stopwatch.GetTimestamp();
                        if (shutdownWaitHandle.WaitOne((int)sleepMilliseconds)) break;

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
                if (shutdown.IsSet) break;

                long tickStart = Stopwatch.GetTimestamp();
                maximumTickLateness = Math.Max(maximumTickLateness, tickStart - nextTick);
                server.Tick();
                long tickDurationTicks = Stopwatch.GetTimestamp() - tickStart;
                slowestTickDuration = Math.Max(slowestTickDuration, tickDurationTicks);
                if (ticks == 0) {
                    reportStart = tickStart;
                    ticks = 1;
                }
                else {
                    ticks++;
                    long reportDuration = tickStart - reportStart;
                    if (reportDuration >= Stopwatch.Frequency) {
                        double ticksPerSecond = (ticks - 1) * (double)Stopwatch.Frequency / reportDuration;
                        if (ticksPerSecond < MinimumTickRate)
                            Log.Warn(
                                $"Server tick rate is below 19 TPS: {ticksPerSecond:F2} TPS " +
                                $"({1000 / ticksPerSecond:F2} ms average, " +
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
        }
        finally {
            Console.CancelKeyPress -= cancel;
        }
    }
}
