using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Commands;

public sealed class CommandContext {
    public readonly Server Server;
    public readonly Player? Player;
    public readonly CommandExecutor Executor;
    internal CommandParameter[] Parameters = [];
    internal object?[] Arguments = [];

    public CommandContext(Server server, Player? player = null)
        : this(player is null ? new ServerCommandExecutor(server) : new PlayerCommandExecutor(player)) {
        if (!ReferenceEquals(Server, server))
            throw new ArgumentException("The player belongs to another server.", nameof(player));
    }

    public CommandContext(CommandExecutor executor) {
        ArgumentNullException.ThrowIfNull(executor);
        Executor = executor;
        Server = executor.Server;
        Player = executor.Player;
    }

    public T Get<T>(string name, T fallback = default!) {
        for (int index = 0; index < Parameters.Length; index++) {
            if (Parameters[index].Name == name) {
                if (Arguments[index] is null)
                    return fallback;
                return Arguments[index] is T value ? value :
                    throw new InvalidCastException($"Command parameter '{name}' is not {typeof(T).Name}.");
            }
        }
        throw new KeyNotFoundException($"Command parameter '{name}' does not exist.");
    }
}
