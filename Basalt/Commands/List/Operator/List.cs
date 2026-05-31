namespace Basalt.Server.Commands.List.Operator;

using Basalt.Server.Commands;

public class ListCommand : Command
{
    public ListCommand() : base("list", "Get a list of players on the server") { }

    public override CommandResult Execute(CommandExecutionState state)
    {
        var playerCount = state.Server.Players.Count();


        var message = $"§r§7Online Players (§a{playerCount}§7)";
        if(playerCount > 0) message += "\n";

        foreach (Player.Player player in state.Server.Players.Values)
        {
            var isLast = player == state.Server.Players.Values.Last();
            if (!isLast)
                message += $"§a{player.Username}, \n";
            else
                message += $"§a{player.Username}";
        }

        return CommandResult.Message(message, true);
    }
}







