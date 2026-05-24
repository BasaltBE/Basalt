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

            server.On<PlayerPlaceBlockSignal>(ServerEvent.PlayerPlaceBlock, signal =>
            {
                signal.Cancel();
            });

            server.On<PlayerBreakBlockSignal>(ServerEvent.PlayerBreakBlock, signal =>
            {
                signal.Cancel();
            });

            server.On<PlayerJoinSignal>(ServerEvent.PlayerJoin, signal =>
{
    signal.Cancel();
});

            server.On<PlayerSpawnSignal>(ServerEvent.PlayerSpawn, signal =>
            {
                signal.Cancel();
            });


            server.Start();
            shutdown.Wait();
            server.Stop();
        }
    }
}
