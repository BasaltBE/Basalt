namespace Basalt.Core.Commands.Vanilla;

using Basalt.Core.Player;

public static class BanCommand {
    public static readonly CommandDefinition Definition = new() {
        Name = "ban",
        Description = "Bans a player.",
        Permissions = ["basalt.op"],
        Overloads = [new OverloadDefinition { Parameters = [
            new ParameterDefinition { Name = "target", Type = typeof(TargetEnum) },
            new ParameterDefinition { Name = "minutes", Type = typeof(IntEnum), Optional = true },
            new ParameterDefinition { Name = "reason", Type = typeof(StringEnum), Optional = true }
        ] }],
        Handler = new CommandHandler(Execute)
    };

    private static CommandResult Execute(CommandContext ctx) {
        TargetEnum? target = ctx.Get<TargetEnum>("target");
        if (target is null) return CommandResult.Error("Usage: /ban <player> [minutes] [reason]");
        Player? player = target.GetSinglePlayer(out CommandResult? error);

        int minutes = ctx.Get<IntEnum>("minutes")?.Value ?? 0;
        if (minutes < 0) return CommandResult.Error("Ban duration cannot be negative.");
        string reason = ctx.Get<StringEnum>("reason")?.Value ?? string.Empty;
        DateTimeOffset? until = minutes == 0 ? null : DateTimeOffset.UtcNow.AddMinutes(minutes);

        if (player is null) {
            if (target.Raw.StartsWith('@')) return error!;
            ctx.Server.BanPlayer(target.Raw, until, reason);
            return CommandResult.OkMessage($"Banned {target.Raw}.");
        }

        ctx.Server.BanPlayer(player, until, reason);
        return CommandResult.OkMessage($"Banned {player.Username}.");
    }
}
