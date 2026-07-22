namespace Basalt.Core.Commands;

using EntityInstance = Entities.Entity;
using Player = Player.Player;

public sealed class TargetEnum : CommandEnum {
    public string Raw { get; private set; } = string.Empty;
    public EntityInstance[] Entities { get; private set; } = [];

    public TargetEnum() : base("target") { }

    public override bool Parse(CommandContext ctx, string[] tokens, ref int tokenIndex) {
        if (tokenIndex >= tokens.Length)
            return false;

        Raw = tokens[tokenIndex];
        Entities = ctx.ResolveTargets(Raw);
        tokenIndex++;
        return true;
    }

    /// <summary>
    /// Get matched players only.
    /// </summary>
    public List<Player> GetPlayers() {
        List<Player> players = [];
        for (int i = 0; i < Entities.Length; i++) {
            if (Entities[i] is Player player)
                players.Add(player);
        }
        return players;
    }

    /// <summary>
    /// Get a single matched player, or null with an error result.
    /// </summary>
    public Player? GetSinglePlayer(out CommandResult? error) {
        List<Player> players = GetPlayers();
        if (players.Count == 0) {
            error = CommandResult.Error("No player found matching the target selector.");
            return null;
        }
        if (players.Count > 1) {
            error = CommandResult.Error("Too many targets matched. Be more specific.");
            return null;
        }
        error = null;
        return players[0];
    }
}
