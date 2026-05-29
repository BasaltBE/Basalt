using Basalt.ServerConsole;
using Basalt.Events;

namespace Basalt.Core
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Logger.Init();
            Server server = new(new ServerOptions { OfflineMode = true });
            using ManualResetEventSlim shutdown = new(false);
            using CancellationTokenSource consoleCancellation = new();

            System.Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                shutdown.Set();
            };

            server.Start();
            ConsoleInterface.Start(server, consoleCancellation.Token, shutdown.Set);
            shutdown.Wait();
            consoleCancellation.Cancel();
            server.Stop();
        }
    }
}
