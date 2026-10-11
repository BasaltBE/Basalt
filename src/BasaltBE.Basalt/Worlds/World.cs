using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;

namespace BasaltBE.Basalt.Worlds;

public sealed class World(string identifier) {
    public readonly string Identifier = identifier;
    public readonly Dictionary<string, Dimension> Dimensions = new(StringComparer.OrdinalIgnoreCase);
    public readonly Dictionary<string, Player> Players = new(StringComparer.Ordinal);

    public Player? GetPlayer(string username) {
        foreach (Player player in Players.Values) {
            if (string.Equals(player.Username, username, StringComparison.OrdinalIgnoreCase))
                return player;
        }

        return null;
    }

    public Player? GetPlayerByXuid(string xuid) => Players.GetValueOrDefault(xuid);
}
