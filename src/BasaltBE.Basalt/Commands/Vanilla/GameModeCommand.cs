using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class GameModeCommand : Command {
    public GameModeCommand() : base("gamemode", "Change a player's game mode.", "basalt.command.gamemode") {
        AddOverload(
            new("mode", new StringEnum()),
            new("player", new TargetEnum(playersOnly: true), optional: true)
        );
    }

    public override CommandResult Execute(CommandContext context) {
        string mode = context.Get<string>("mode").ToLowerInvariant();
        GameType gameType = mode switch {
            "s" or "survival" => GameType.Survival,
            "c" or "creative" => GameType.Creative,
            "a" or "adventure" => GameType.Adventure,
            "sp" or "spectator" => GameType.Spectator,
            _ => GameType.Undefined
        };
        if (gameType == GameType.Undefined)
            return new(false, "Use survival, creative, adventure, or spectator.");

        Entity[]? selected = context.Get<Entity[]?>("player");
        if (selected is null && context.Player is { } executor)
            selected = [executor];
        if (selected is null || selected.Length == 0)
            return new(false, "Select at least one player.");
        if (selected.Length > 128)
            return new(false, "Select no more than 128 players.");

        int changed = 0;
        foreach (Entity entity in selected) {
            if (entity is Player { Spawned: true, Disposed: false } player) {
                player.SetGameType(gameType);
                changed++;
            }
        }

        return new(changed > 0, $"Set {changed} player(s) to {gameType.ToString().ToLowerInvariant()}.");
    }
}
