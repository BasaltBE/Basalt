namespace Basalt.Core.Commands.Vanilla;

using Player = Player.Player;

public static class XpCommand {
    public static readonly CommandDefinition Definition = new() {
        Name = "xp",
        Description = "Adds experience points or levels to a player.",
        Permissions = ["basalt.op"],
        Overloads = [
            new OverloadDefinition {
                Parameters = [
                    new ParameterDefinition { Name = "amount", Type = typeof(XpAmountEnum) },
                    new ParameterDefinition { Name = "player", Type = typeof(TargetEnum), Optional = true }
                ]
            }
        ],
        Handler = new CommandHandler(Execute)
    };

    private static CommandResult Execute(CommandContext ctx) {
        XpAmountEnum? amount = ctx.Get<XpAmountEnum>("amount");
        TargetEnum? target = ctx.Get<TargetEnum>("player");
        if (amount is null) return CommandResult.Error("Usage: /xp <amount>[L] [player]");

        List<Player> players = target?.GetPlayers() ?? (ctx.Sender.AsPlayer() is { } self ? [self] : []);
        if (players.Count == 0) return CommandResult.Error("No player found matching the target selector.");

        foreach (Player player in players) {
            if (amount.Levels) {
                player.AddExperienceLevels(amount.Value);
            }
            else if (amount.Value >= 0) {
                player.AddExperience(amount.Value);
            }
            else {
                player.RemoveExperience(-amount.Value);
            }
        }

        string unit = amount.Levels ? "levels" : "experience";
        return CommandResult.OkMessage($"§7Added §a{amount.Value} §7{unit} to §a{players.Count} §7player(s).");
    }
}
