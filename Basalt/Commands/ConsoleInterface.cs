using Basalt.Server.Commands;
using Basalt.Server;

namespace Basalt.Server.Commands;

public static class ConsoleInterface
{
    public static void Start(global::Basalt.Server.Server server, CancellationToken cancellationToken, Action requestShutdown)
    {
        Task.Run(() =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string? line = System.Console.ReadLine();
                if (line is null)
                {
                    continue;
                }

                string[] tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (tokens.Length == 0)
                {
                    continue;
                }

                if (tokens[0].Equals("stop", StringComparison.OrdinalIgnoreCase))
                {
                    requestShutdown();
                    return;
                }

                HandleResult(server.Commands.Execute(server, line));
            }
        }, cancellationToken);
    }

    static void HandleResult(CommandResult result)
    {
        for (int i = 0; i < result.Messages.Count; i++)
        {
            Logger.Info(result.Messages[i]);
        }
    }
}
