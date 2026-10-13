using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Types;

namespace BasaltBE.Basalt.Worlds;

public sealed class World : IDisposable {
    readonly Lock playersSync = new();
    readonly Dictionary<string, Player> playersByUsername = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, Player> playersByXuid = new(StringComparer.Ordinal);
    readonly ChunkGeneratorRegistry generators;
    readonly ChunkGenerationService generationService;

    public readonly string Identifier;
    public readonly Dictionary<string, Dimension> Dimensions = new(StringComparer.OrdinalIgnoreCase);

    public World(string identifier, int workerCount = 4, int queueCapacity = 1024)
        : this(identifier, new ChunkGeneratorRegistry(), workerCount, queueCapacity) { }

    public World(string identifier, ChunkGeneratorRegistry generators, int workerCount = 4, int queueCapacity = 1024) {
        ArgumentException.ThrowIfNullOrEmpty(identifier);
        ArgumentNullException.ThrowIfNull(generators);
        Identifier = identifier;
        this.generators = generators;
        generationService = new ChunkGenerationService(workerCount, queueCapacity);
    }

    public Dimension? GetDimension(string identifier) => Dimensions.GetValueOrDefault(identifier);

    public Dimension CreateDimension(string identifier) {
        ArgumentException.ThrowIfNullOrEmpty(identifier);

        if (Dimensions.ContainsKey(identifier)) {
            throw new InvalidOperationException($"Dimension '{identifier}' already exists in world '{Identifier}'.");
        }

        Dimension dimension = new(identifier, generators.CreateFor(identifier), generationService);
        Dimensions.Add(identifier, dimension);
        return dimension;
    }

    public void AddPlayer(Player player) {
        lock (playersSync) {
            if (playersByXuid.ContainsKey(player.Xuid) || playersByUsername.ContainsKey(player.Username)) {
                throw new InvalidOperationException("A player with the same XUID or username is already in the world.");
            }

            playersByXuid.Add(player.Xuid, player);
            playersByUsername.Add(player.Username, player);
        }
    }

    public Player? GetPlayer(string username) {
        lock (playersSync)
            return playersByUsername.GetValueOrDefault(username);
    }

    public Player? GetPlayerByXuid(string xuid) {
        lock (playersSync)
            return playersByXuid.GetValueOrDefault(xuid);
    }

    public void RemovePlayer(Player player) {
        lock (playersSync) {
            if (playersByXuid.TryGetValue(player.Xuid, out Player? byXuid) && ReferenceEquals(byXuid, player))
                playersByXuid.Remove(player.Xuid);
            if (playersByUsername.TryGetValue(player.Username, out Player? byUsername) && ReferenceEquals(byUsername, player))
                playersByUsername.Remove(player.Username);
        }
    }

    public void Tick() {
        foreach (Dimension dimension in Dimensions.Values)
            dimension.DrainGeneratedChunks();
    }

    public void Dispose() => generationService.Dispose();
}
