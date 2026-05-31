namespace Basalt.Server.Commands.List.Operator;

using Basalt.Server.Commands;

public class DeopCommand : Command
{
    public DeopCommand() : base("deop", "Revokes operator status from a player.")
    {
        Permissions.Add("basalt.op");

        CreateOverload()
            .Set<TargetEnum>("target", true);
    }

    public override CommandResult Execute(CommandExecutionState state)
    {
        TargetEnum? target = state.Get<TargetEnum>("target");
        if (target is null)
        {
            return CommandResult.Empty(false);
        }

        if (target.Entities.Length > 1)
        {
            return CommandResult.Message("Multiple players matched the target selector, please be more specific", false);
        }

        if (target.Entities.Length == 1 && target.Entities[0] is global::Basalt.Server.Player.Player player)
        {
            player.SetOperator(false);
            return CommandResult.Message($"Removed {player.Username} from server operators.", true);
        }

        return CommandResult.Message("No online players matched the target selector", false);
    }
}
