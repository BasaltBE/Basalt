namespace Basalt.Server.Commands.List.Operator;

using Basalt.Server.Commands;

public class OpCommand : Command
{
    public OpCommand() : base("op", "Grants operator status to a player.")
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
            player.SetOperator(true);
            return CommandResult.Message($"Made {player.Username} a server operator.", true);
        }

        return CommandResult.Message("No online players matched the target selector", false);
    }
}
