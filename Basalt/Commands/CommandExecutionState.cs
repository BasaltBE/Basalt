using Basalt.Core;

namespace Basalt.Commands;

public interface ICommandExecutor { }

/// <summary>
/// The Server execited a command
/// </summary>
public class ServerExecutor : ICommandExecutor
{
    public void SendMessage(string message)
    {   
        Logger.Chat(message);
    }
}

/// <summary>
/// The Player executed a command
/// </summary>
public class PlayerExecutor : ICommandExecutor
{
    public required Player Player { get; init; }

    public void SendMessage(string message)
    {
        Player.SendMessage(message);
    }
}

public sealed class CommandExecutionState
{
    /// <summary>
    /// The command string that was executed.
    /// </summary>
    public required string Command { get; init; }

    public required ICommandExecutor Executor { get; init; }

    public required Server Server { get; init; }

    public CommandOverload? Overload;

    public List<CommandArgument> Arguments = new();

    public T? Get<T>(string name) where T : CommandEnum
    {
        for (int i = 0; i < Arguments.Count; i++)
        {
            CommandArgument argument = Arguments[i];
            if (argument.Name == name && argument.Value is T value)
            {
                return value;
            }
        }

        if (Overload is null)
        {
            throw new KeyNotFoundException($"Command argument '{name}' was not registered.");
        }

        for (int i = 0; i < Overload.Parameters.Count; i++)
        {
            CommandParameter parameter = Overload.Parameters[i];
            if (parameter.Name != name || parameter.Enum != typeof(T))
            {
                continue;
            }

            if (parameter.Required)
            {
                throw new KeyNotFoundException($"Required command argument '{name}' was not found.");
            }

            return null;
        }

        throw new KeyNotFoundException($"Command argument '{name}' was not registered.");
    }
}
