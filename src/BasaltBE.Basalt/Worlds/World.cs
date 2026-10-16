using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Events;
using BasaltBE.Basalt.Events.Worlds;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Types;

namespace BasaltBE.Basalt.Worlds;

public sealed class World : IDisposable {
    readonly Lock playersSync = new();
    readonly Dictionary<string, Player> playersByUsername = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, Player> playersByXuid = new(StringComparer.Ordinal);
    readonly ChunkGeneratorRegistry generators;
    readonly ChunkGenerationService generationService;
    int initialization;

    public readonly string Identifier;
    public readonly Dictionary<string, Dimension> Dimensions = new(StringComparer.OrdinalIgnoreCase);
    public bool Initialized => Volatile.Read(ref initialization) == 2;

    public World(string identifier, ChunkGeneratorRegistry generators, int workerCount = 4, int queueCapacity = 1024) {
        ArgumentException.ThrowIfNullOrEmpty(identifier);
        ArgumentNullException.ThrowIfNull(generators);
        Identifier = identifier;
        this.generators = generators;
        generationService = new ChunkGenerationService(workerCount, queueCapacity);
    }

    internal void Initialize(Action<World> configure, EventEmitter events) {
        if (Interlocked.CompareExchange(ref initialization, 1, 0) != 0)
            throw new InvalidOperationException($"World '{Identifier}' initialization has already started.");

        configure(this);
        if (Dimensions.Count == 0)
            throw new InvalidOperationException($"World '{Identifier}' requires at least one dimension.");

        Volatile.Write(ref initialization, 2);
        if (events.HasSubscribers<WorldInitializeEvent>())
            events.Emit(new WorldInitializeEvent(this));
    }

    public Dimension? GetDimension(string identifier) => Dimensions.GetValueOrDefault(identifier);

    public Dimension CreateDimension(string identifier) {
        return CreateDimension(identifier, generators.CreateDefault());
    }

    public Dimension CreateDimension(string identifier, string generatorIdentifier) {
        return CreateDimension(identifier, generators.Create(generatorIdentifier));
    }

    Dimension CreateDimension(string identifier, IChunkGenerator generator) {
        ArgumentException.ThrowIfNullOrEmpty(identifier);

        if (Dimensions.ContainsKey(identifier)) {
            throw new InvalidOperationException($"Dimension '{identifier}' already exists in world '{Identifier}'.");
        }

        Dimension dimension = new(identifier, generator, generationService);
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
            player.Dimension.AddPlayer(player);
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
            player.Dimension.RemovePlayer(player);
        }
    }

    public void Tick() {
        foreach (Dimension dimension in Dimensions.Values)
            dimension.DrainGeneratedChunks();
    }

    public void Dispose() => generationService.Dispose();
}
