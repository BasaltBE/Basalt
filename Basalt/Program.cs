using Basalt.Events;

namespace Basalt.Core
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Logger.Init();
            Server server = new();
            using ManualResetEventSlim shutdown = new(false);

            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                shutdown.Set();
            };


            server.Start();
            shutdown.Wait();
            server.Stop();
        }
    }
}
