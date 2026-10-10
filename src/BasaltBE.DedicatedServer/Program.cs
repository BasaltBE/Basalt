using BasaltBE.Basalt;
using BasaltBE.Logging;
using BasaltBE.Logging.Enums;
using BasaltBE.NetherNet.Types;

namespace BasaltBE.DedicatedServer;

internal static class Program {
    private static void Main() {
        Logger.MinimumLevel = LogLevel.Debug;
        using ManualResetEventSlim shutdown = new(false);
        ConsoleCancelEventHandler cancel = (_, eventArgs) => {
            eventArgs.Cancel = true;
            Logger.Scoped("DedicatedServer").Info("Shutdown requested.");
            shutdown.Set();
        };
        Console.CancelKeyPress += cancel;

        try {
            using Server server = new(new NethernetServerOptions {
                Advertisement = new ServerAdvertisement {
                    Name = "BasaltBE",
                    Protocol = 2223,
                    Version = "0.0.0",
                    Level = "Bedrock level",
                    MaxPlayers = 100
                }
            });

            server.Start();
            Logger.Scoped("DedicatedServer").Info($"NetherNet is listening on network {server.Network.NetworkId}.");
            shutdown.Wait();
        }
        finally {
            Console.CancelKeyPress -= cancel;
        }
    }
}
