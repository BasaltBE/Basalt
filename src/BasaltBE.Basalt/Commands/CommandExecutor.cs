using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Commands;

public abstract class CommandExecutor
{
    public readonly Server Server;
    public readonly Player? Player;

    private protected CommandExecutor(Server server, Player? player)
    {
        ArgumentNullException.ThrowIfNull(server);
        Server = server;
        Player = player;
    }

    public abstract bool Allows(string permission);

    public CommandResult Execute(string command) => Server.Commands.Execute(command, this);
}
