using Basalt.Commands;
using Basalt.Commands.List.Operator;
using Basalt.Core;

namespace Basalt.ServerConsole;

public static class ConsoleInterface
{
    public static void Start(Server server, CancellationToken cancellationToken, Action requestShutdown)
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

                switch (tokens[0].ToLowerInvariant())
                {
                    case "stop":
                        requestShutdown();
                        return;
                    case "op" when tokens.Length >= 2:
                        HandleResult(OperatorActions.GrantOperator(server, tokens[1]));
                        break;
                    case "deop" when tokens.Length >= 2:
                        HandleResult(OperatorActions.RevokeOperator(server, tokens[1]));
                        break;
                    default:
                        HandleResult(server.Commands.Execute(server, line));
                        break;
                }
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
