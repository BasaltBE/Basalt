using Basalt.Commands;
using Basalt.Core;

namespace Basalt.Commands.List.Operator;

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

        if (target.Entities.Length > 1 || target.OfflineUsernames.Length > 1)
        {
            return CommandResult.Message("§cMultiple players matched the target selector, please be more specific", false);
        }

        if (target.Entities.Length == 1 && target.Entities[0] is Player player)
        {
            return OperatorActions.RevokeOperator(state.Server, player.Username);
        }

        if (target.OfflineUsernames.Length == 1)
        {
            return OperatorActions.RevokeOperator(state.Server, target.OfflineUsernames[0]);
        }

        return CommandResult.Message("§cNo players matched the target selector", false);
    }
}
