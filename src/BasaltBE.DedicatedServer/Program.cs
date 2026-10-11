using System.Diagnostics;
using BasaltBE.Basalt;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Basalt.Properties;
using BasaltBE.Logging;
using BasaltBE.Logging.Enums;

namespace BasaltBE.DedicatedServer;

internal static class Program {
    private const int TickRate = 20;
    private const int TickSpinMilliseconds = 25;
    private const double MinimumTickRate = 19;
    private static readonly ScopedLogger Log = Logger.Scoped("DedicatedServer");

    private static void Main(string[] args) {
        Profiler.Enabled = args.Contains("--tracy", StringComparer.OrdinalIgnoreCase);
        Profiler.SetThreadName("DedicatedServer");
        Logger.MinimumLevel = LogLevel.Debug;
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

            using Server server = new(properties);

            server.Start();
            Log.Info($"NetherNet is listening on network {server.Network.NetworkId}.");

            long tickDuration = Stopwatch.Frequency / TickRate;
            long spinDuration = Stopwatch.Frequency * TickSpinMilliseconds / 1000;
            long sleepDuration = tickDuration - spinDuration;
            long reportStart = Stopwatch.GetTimestamp();
            int ticks = 0;
            while (!shutdown.IsSet) {
                long tickStart = Stopwatch.GetTimestamp();
                server.Tick();
                long tickCompleted = Stopwatch.GetTimestamp();
                long tickElapsed = tickCompleted - tickStart;
                ticks++;

                long now = tickCompleted;
                long reportDuration = now - reportStart;
                if (reportDuration >= Stopwatch.Frequency) {
                    double ticksPerSecond = ticks * (double)Stopwatch.Frequency / reportDuration;
                    if (ticksPerSecond < MinimumTickRate)
                        Log.Warn($"Server tick rate is below 19 TPS: {ticksPerSecond:F2} TPS.");

                    ticks = 0;
                    reportStart = now;
                }

                long sleepRemaining = sleepDuration - tickElapsed - (Stopwatch.GetTimestamp() - tickCompleted);
                int sleepMilliseconds = (int)(sleepRemaining * 1000 / Stopwatch.Frequency);
                if (sleepMilliseconds > 0) Thread.Sleep(sleepMilliseconds);

                long tickEnd = tickStart + tickDuration;
                while (Stopwatch.GetTimestamp() < tickEnd) Thread.SpinWait(1);
            }
        }
        finally {
            Console.CancelKeyPress -= cancel;
        }
    }
}
