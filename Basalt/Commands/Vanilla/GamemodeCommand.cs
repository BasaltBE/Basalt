namespace Basalt.Core.Commands.Vanilla;

using Basalt.BedrockProtocol.Enums;
using Player = Player.Player;

public sealed class GamemodeEnum : CustomEnum {
    public static readonly string[] Values =
    [
        "survival", "s", "0",
        "creative", "c", "1",
        "adventure", "a", "2",
        "spectator", "sp", "6"
    ];

    public GamemodeEnum() : base("GameMode", Values) { }

    public GameType ToGamemode() => Value?.ToLowerInvariant() switch {
        "survival" or "s" or "0" => GameType.Survival,
        "creative" or "c" or "1" => GameType.Creative,
        "adventure" or "a" or "2" => GameType.Adventure,
        "spectator" or "sp" or "6" => GameType.Spectator,
        _ => GameType.Survival
    };
}

public static class GamemodeCommand {
    public static readonly CommandDefinition Definition = new() {
        Name = "gamemode",
        Description = "Changes the game mode for a player.",
        Aliases = [],
        Permissions = ["basalt.op"],
        Overloads =
        [
            new OverloadDefinition
            {
                Parameters =
                [
                    new ParameterDefinition { Name = "gameMode", Type = typeof(GamemodeEnum) }
                ]
            },
            new OverloadDefinition
            {
                Parameters =
                [
                    new ParameterDefinition { Name = "gameMode", Type = typeof(GamemodeEnum) },
                    new ParameterDefinition { Name = "player", Type = typeof(TargetEnum), Optional = true }
                ]
            }
        ],
        Handler = new CommandHandler(Execute)
    };

    static CommandResult Execute(CommandContext ctx) {
        GamemodeEnum? gamemode = ctx.Get<GamemodeEnum>("gameMode");
        if (gamemode is null)
            return CommandResult.Error("Usage: /gamemode <survival|creative|adventure|spectator> [player]");

        GameType mode = gamemode.ToGamemode();

        TargetEnum? target = ctx.Get<TargetEnum>("player");
        if (target is not null) {
            Player? player = target.GetSinglePlayer(out CommandResult? error);
            if (player is null) return error!;

            player.SetGamemode(mode);
            return CommandResult.OkMessage($"Set {player.Username}'s game mode to {gamemode.Value}.");
        }

        Player? self = ctx.RequirePlayer(out CommandResult? selfError);
        if (self is null) return selfError!;

        self.SetGamemode(mode);
        return CommandResult.OkMessage($"Set your game mode to {gamemode.Value}.");
    }
}
