namespace Basalt.Core.Commands.Vanilla;

using Player = Player.Player;

public static class DeopCommand {
    public static readonly CommandDefinition Definition = new() {
        Name = "deop",
        Description = "Revokes operator status from a player.",
        Permissions = ["basalt.op"],
        Overloads =
        [
            new OverloadDefinition
            {
                Parameters =
                [
                    new ParameterDefinition { Name = "target", Type = typeof(TargetEnum) }
                ]
            }
        ],
        Handler = new CommandHandler(Execute)
    };

    static CommandResult Execute(CommandContext ctx) {
        TargetEnum? target = ctx.Get<TargetEnum>("target");
        if (target is null)
            return CommandResult.Error("Usage: /deop <player>");

        Player? player = target.GetSinglePlayer(out CommandResult? error);
        if (player is null) return error!;

        player.SetOperator(false);
        return CommandResult.OkMessage($"Removed {player.Username} from server operators.");
    }
}
