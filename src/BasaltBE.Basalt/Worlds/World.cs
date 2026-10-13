using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;

namespace BasaltBE.Basalt.Worlds;

public sealed class World(string identifier) {
    readonly Dictionary<string, Player> playersByUsername = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, Player> playersByXuid = new(StringComparer.Ordinal);

    public readonly string Identifier = identifier;
    public readonly Dictionary<string, Dimension> Dimensions = new(StringComparer.OrdinalIgnoreCase);

    public Dimension? GetDimension(string identifier) => Dimensions.GetValueOrDefault(identifier);

    public Dimension GetOrCreateDimension(string identifier) {
        if (Dimensions.TryGetValue(identifier, out Dimension? dimension)) {
            return dimension;
        }

        dimension = new Dimension(identifier);
        Dimensions.Add(identifier, dimension);
        return dimension;
    }

    public void AddPlayer(Player player) {
        if (playersByXuid.ContainsKey(player.Xuid) || playersByUsername.ContainsKey(player.Username)) {
            throw new InvalidOperationException("A player with the same XUID or username is already in the world.");
        }

        playersByXuid.Add(player.Xuid, player);
        playersByUsername.Add(player.Username, player);
    }

    public Player? GetPlayer(string username) => playersByUsername.GetValueOrDefault(username);

    public Player? GetPlayerByXuid(string xuid) => playersByXuid.GetValueOrDefault(xuid);
}
