namespace Basalt.Core.Commands.Vanilla;

using Basalt.Core.Player;

public static class KickCommand {
    public static readonly CommandDefinition Definition = new() {
        Name = "kick",
        Description = "Kicks a player.",
        Permissions = ["basalt.op"],
        Overloads = [new OverloadDefinition { Parameters = [
            new ParameterDefinition { Name = "target", Type = typeof(TargetEnum) },
            new ParameterDefinition { Name = "reason", Type = typeof(StringEnum), Optional = true }
        ] }],
        Handler = new CommandHandler(Execute)
    };

    private static CommandResult Execute(CommandContext ctx) {
        TargetEnum? target = ctx.Get<TargetEnum>("target");
        if (target is null) return CommandResult.Error("Usage: /kick <player> [reason]");
        Player[] players = target.GetPlayers().ToArray();
        if (players.Length == 0) return CommandResult.Error("No player found matching the target selector.");
        string reason = ctx.Get<StringEnum>("reason")?.Value ?? "Kicked by an operator.";
        foreach (Player player in players) ctx.Server.KickPlayer(player, reason);
        string suffix = players.Length == 1 ? string.Empty : "s";
        return CommandResult.OkMessage($"Kicked {players.Length} player{suffix}.");
    }
}
