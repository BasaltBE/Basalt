namespace Basalt.Core
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Logger.Init();
            Server server = new();

            Logger.Info("Basalt listening on 0.0.0.0:19132");
            server.Start();
        }
    }
}
