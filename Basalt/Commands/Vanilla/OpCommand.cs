namespace Basalt.Core.Commands.Vanilla;

using Player = Player.Player;

public static class OpCommand
{
    public static readonly CommandDefinition Definition = new()
    {
        Name = "op",
        Description = "Grants operator status to a player.",
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

    static CommandResult Execute(CommandContext ctx)
    {
        TargetEnum? target = ctx.Get<TargetEnum>("target");
        if (target is null)
            return CommandResult.Error("Usage: /op <player>");

        Player? player = target.GetSinglePlayer(out CommandResult? error);
        if (player is null) return error!;

        player.SetOperator(true);
        return CommandResult.OkMessage($"Made {player.Username} a server operator.");
    }
}
