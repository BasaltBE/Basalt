namespace Basalt.Core
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Server server = new();

            Console.WriteLine("Basalt RakNet listening on 0.0.0.0:19132");
            server.Start();
        }
    }
}
