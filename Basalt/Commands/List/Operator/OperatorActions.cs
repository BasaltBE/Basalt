using Basalt.Commands;
using Basalt.Core;
using Basalt.World;

namespace Basalt.Commands.List.Operator;

public static class OperatorActions
{
    public static CommandResult GrantOperator(Server server, string targetName)
    {
        Basalt.World.World world = server.GetWorld();

        foreach (Player player in server.Players.Values)
        {
            if (!string.Equals(player.Username, targetName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            world.Operators.AddOperator(player.Xuid);
            player.SetOperator(true);
            world.PlayerProfiles.TryUpdateOperator(player.Username, true);
            return CommandResult.Message($"§7Made §a{player.Username} §7a server operator.", true);
        }

        if (OfflinePlayerActions.TryGrantOperator(world, targetName))
        {
            return CommandResult.Message($"§7Made §a{targetName} §7a server operator.", true);
        }

        return CommandResult.Message("§cThat player has never joined this server.", false);
    }

    public static CommandResult RevokeOperator(Server server, string targetName)
    {
        Basalt.World.World world = server.GetWorld();

        foreach (Player player in server.Players.Values)
        {
            if (!string.Equals(player.Username, targetName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            world.Operators.RemoveOperator(player.Xuid);
            player.SetOperator(false);
            world.PlayerProfiles.TryUpdateOperator(player.Username, false);
            return CommandResult.Message($"§7Removed §a{player.Username} §7from server operators.", true);
        }

        if (OfflinePlayerActions.TryRevokeOperator(world, targetName))
        {
            return CommandResult.Message($"§7Removed §a{targetName} §7from server operators.", true);
        }

        return CommandResult.Message("§cThat player has never joined this server.", false);
    }
}
