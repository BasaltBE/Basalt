namespace Basalt.Core.Commands.Vanilla;

public static class UnBanCommand {
    public static readonly CommandDefinition Definition = new() {
        Name = "unban",
        Description = "Removes a player's ban.",
        Permissions = ["basalt.op"],
        Overloads = [new OverloadDefinition { Parameters = [
            new ParameterDefinition { Name = "player", Type = typeof(StringEnum) }
        ] }],
        Handler = new CommandHandler(Execute)
    };

    private static CommandResult Execute(CommandContext ctx) {
        string? identifier = ctx.Get<StringEnum>("player")?.Value;
        if (string.IsNullOrWhiteSpace(identifier)) return CommandResult.Error("Usage: /unban <player>");
        return ctx.Server.UnBanPlayer(identifier)
            ? CommandResult.OkMessage($"Unbanned {identifier}.")
            : CommandResult.Error($"{identifier} is not banned.");
    }
}
