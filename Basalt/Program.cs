using Basalt.RakNet;

namespace Basalt.Core
{
    internal class Program
    {
        static void Main(string[] args)
        {
            NetworkServer server = new();
            server.OnMessage += (_, payload) =>
            {
                if (payload.Length == 0)
                {
                    return;
                }

                byte packetId = payload.Span[0];
                Console.WriteLine($"Game packet 0x{packetId:X2} ({payload.Length} bytes)");
            };

            Console.WriteLine("`Basalt RakNet listening on 0.0.0.0:19132");
            //Server.Listen(19132);
            server.Start().AsTask().Wait();
        }
    }
}
