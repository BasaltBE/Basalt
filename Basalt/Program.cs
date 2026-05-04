using Basalt.RakNet;

namespace Basalt.Core
{
    internal class Program
    {
        static void Main(string[] args)
        {
            NetworkServer Server = new();
            Console.WriteLine("`Basalt RakNet listening on 0.0.0.0:19132");
            Server.Listen(19132);
        }
    }
}
