namespace Basalt.Core.Commands;

public static class ConsoleInterface
{
    public static void Start(Server server, CancellationToken cancellationToken, Action requestShutdown)
    {
        Task.Run(() =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    string? line = System.Console.ReadLine();
                    if (line is null)
                        continue;

                    string trimmed = line.Trim();
                    if (trimmed.Length == 0)
                        continue;

                    if (trimmed.Equals("stop", StringComparison.OrdinalIgnoreCase))
                    {
                        requestShutdown();
                        return;
                    }

                    CommandResult result = server.Commands.Execute(server, trimmed);
                    if (result.Message is not null)
                    {
                        Logger.Chat(result.Message);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Err(ex.ToString());
                }
            }
        }, cancellationToken);
    }
}
