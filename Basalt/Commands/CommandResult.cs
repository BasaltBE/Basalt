namespace Basalt.Server.Commands;

public sealed class CommandResult
{
    public bool Success;

    public List<string> Messages = [];

    public static CommandResult Empty(bool success = true)
    {
        return new CommandResult { Success = success };
    }

    public static CommandResult Message(string message, bool success = true)
    {
        return new CommandResult
        {
            Success = success,
            Messages = [message]
        };
    }
}







