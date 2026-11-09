using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class GameModeCommand : Command {
    public GameModeCommand() : base("gamemode", "Sets a player's game mode.", "basalt.command.gamemode") {
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
        if (gameType == GameType.Undefined) return new(false, $"Game mode '{mode}' is invalid.");

        Entity[]? selected = context.Get<Entity[]?>("player");
        if (selected is null && context.Player is { } executor) selected = [executor];
        if (selected is null || selected.Length == 0) return new(false, "No players matched the target.");
        if (selected.Length > 128) return new(false, "Select no more than 128 players.");

        List<string> changed = [];
        foreach (Entity entity in selected) {
            if (entity is not Player { Spawned: true, Disposed: false } player) continue;

            player.SetGameType(gameType);
            changed.Add(player.Username);
        }

        if (changed.Count == 0) return new(false, "No selected players are online and spawned.");

        string gameMode = gameType.ToString().ToLowerInvariant();
        if (context.Player is { } sender && changed.Count == 1 && changed[0] == sender.Username)
            return new(true, $"Set own game mode to {gameMode}.");
        if (changed.Count == 1) return new(true, $"Set {changed[0]}'s game mode to {gameMode}.");
        return new(true, $"Set game mode to {gameMode} for {string.Join(", ", changed)}.");
    }
}
