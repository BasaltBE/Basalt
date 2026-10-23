using System.Diagnostics;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using BasaltBE.NetherNet.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Basalt.Events;
using BasaltBE.Basalt.Events.Worlds;
using BasaltBE.Basalt.LootTables;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Types;
using BasaltBE.Basalt.Worlds.Providers;
using BasaltBE.Basalt.Worlds.Providers.Types;
using BasaltBE.Logging;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Worlds;

public sealed class World : IDisposable {
    internal long ChunkSendBudgetRemaining;
    readonly Lock playersSync = new();
    readonly Dictionary<string, Player> playersByUsername = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, Player> playersByXuid = new(StringComparer.Ordinal);
    readonly ChunkGeneratorRegistry generators;
    readonly ChunkGenerationService generationService;
    readonly bool ownsGenerationService;
    readonly WorldProviderService? storage;
    EventEmitter? events;
    internal Server? Server;
    internal int OwnerThread;
    internal bool Started;
    readonly ConcurrentQueue<(NethernetMessage Message, byte[]? Buffer)> inbound = new();
    long queuedBytes;
    int queuedMessages;

    int initialization;
    internal ulong TickValue;

    public readonly string Identifier;
    public readonly LootTableManager LootTables;
    public readonly TickMetrics Ticks = new();
    public WorldStorageMetrics StorageMetrics => storage?.Metrics ?? default;
    private readonly Dictionary<string, Dimension> dimensions = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyDictionary<string, Dimension> dimensionSnapshot;
    public IReadOnlyDictionary<string, Dimension> Dimensions => Volatile.Read(ref dimensionSnapshot);
    public bool Initialized => Volatile.Read(ref initialization) == 2;

    public World(
        string identifier,
        ChunkGeneratorRegistry generators,
        int workerCount = 4,
        int queueCapacity = 1024,
        LootTableManager? lootTables = null,
        IWorldProvider? provider = null
    ) : this(identifier, generators, null, lootTables, workerCount, queueCapacity, provider) { }

    internal World(
        string identifier,
        ChunkGeneratorRegistry generators,
        ChunkGenerationService? generationService,
        LootTableManager? lootTables,
        int workerCount = 4,
        int queueCapacity = 1024,
        IWorldProvider? provider = null
    ) {
        ArgumentException.ThrowIfNullOrEmpty(identifier);
        ArgumentNullException.ThrowIfNull(generators);
        dimensionSnapshot = new ReadOnlyDictionary<string, Dimension>(new Dictionary<string, Dimension>(dimensions, StringComparer.OrdinalIgnoreCase));
        Identifier = identifier;
        this.generators = generators;
        LootTables = lootTables ?? new LootTableManager();
        ownsGenerationService = generationService is null;
        this.generationService = generationService ?? new ChunkGenerationService(workerCount, queueCapacity);
        storage = provider is null ? null : new WorldProviderService(provider);
    }

    internal WorldProviderService? Storage => storage;

    internal void Initialize(Action<World> configure, EventEmitter events) {
        if (Interlocked.CompareExchange(ref initialization, 1, 0) != 0)
            throw new InvalidOperationException($"World '{Identifier}' initialization has already started.");

        this.events = events;
        configure(this);
        if (Dimensions.Count == 0)
            throw new InvalidOperationException($"World '{Identifier}' requires at least one dimension.");

        Volatile.Write(ref initialization, 2);
        if (events.HasSubscribers<WorldInitializeEvent>())
            events.Emit(new WorldInitializeEvent(this));
    }

    public Dimension? GetDimension(string identifier) => Dimensions.GetValueOrDefault(identifier);

    public Dimension? GetDimension(DimensionType type) {
        foreach (Dimension dimension in Dimensions.Values) {
            if (dimension.Type == type)
                return dimension;
        }

        return null;
    }

    public Dimension CreateDimension(string identifier) =>
        CreateDimension(identifier, Dimension.GetDimensionType(identifier));

    public Dimension CreateDimension(string identifier, string generatorIdentifier) =>
        CreateDimension(identifier, Dimension.GetDimensionType(identifier), generatorIdentifier);

    public Dimension CreateDimension(string identifier, DimensionType type) =>
        CreateDimension(identifier, type, generators.CreateDefault());

    public Dimension CreateDimension(string identifier, DimensionType type, string generatorIdentifier) =>
        CreateDimension(identifier, type, generators.Create(generatorIdentifier));

    Dimension CreateDimension(string identifier, DimensionType type, IChunkGenerator generator) {
        ArgumentException.ThrowIfNullOrEmpty(identifier);
        if (type is not (DimensionType.Overworld or DimensionType.Nether or DimensionType.TheEnd))
            throw new ArgumentOutOfRangeException(nameof(type));
        if (Started)
            CheckOwnership();

        if (Dimensions.ContainsKey(identifier)) {
            throw new InvalidOperationException($"Dimension '{identifier}' already exists in world '{Identifier}'.");
        }

        Dimension dimension = new(identifier, type, generator, generationService);
        dimension.PrimaryStorageDimension = !dimensions.Values.Any(existing => existing.Type == type);
        dimension.Events = events;
        dimension.World = this;
        dimension.SimulationRadius = Server?.Properties.TickDistance ?? 4;
        dimensions.Add(identifier, dimension);
        Volatile.Write(ref dimensionSnapshot,
            new ReadOnlyDictionary<string, Dimension>(new Dictionary<string, Dimension>(dimensions, StringComparer.OrdinalIgnoreCase)));
        return dimension;
    }

    public void AddPlayer(Player player) {
        CheckOwnership();
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
        CheckOwnership();
        lock (playersSync) {
            if (playersByXuid.TryGetValue(player.Xuid, out Player? byXuid) && ReferenceEquals(byXuid, player))
                playersByXuid.Remove(player.Xuid);
            if (playersByUsername.TryGetValue(player.Username, out Player? byUsername) && ReferenceEquals(byUsername, player))
                playersByUsername.Remove(player.Username);
            player.Dimension.RemovePlayer(player);
        }
    }

    internal bool Enqueue(NethernetMessage message) {
        if (message.Disconnected) {
            inbound.Enqueue((message, null));
            return true;
        }
        int length = message.Payload.Length;
        if (Interlocked.Increment(ref queuedMessages) > 4096) {
            Interlocked.Decrement(ref queuedMessages);
            return false;
        }
        if (Interlocked.Add(ref queuedBytes, length) > 16 * 1024 * 1024) {
            Interlocked.Add(ref queuedBytes, -length);
            Interlocked.Decrement(ref queuedMessages);
            return false;
        }
        byte[] buffer = ArrayPool<byte>.Shared.Rent(length);
        message.Payload.Span.CopyTo(buffer);
        inbound.Enqueue((message with { Payload = buffer.AsMemory(0, length) }, buffer));
        return true;
    }

    internal void CheckOwnership() {
        if (Started && Environment.CurrentManagedThreadId != OwnerThread && Server?.Gameplay.IsWriteLockHeld != true)
            throw new InvalidOperationException("Gameplay state belongs to another tick thread.");
    }

    public void Tick() {
        CheckOwnership();

        long start = Stopwatch.GetTimestamp();
        using ProfilerZone zone = Profiler.BeginZone("World.Tick");
        TickValue++;
        Server?.Network.Login.ProcessCompletions(this);

        long ingressDeadline = start + Stopwatch.Frequency / 250;
        for (int count = 0;
             count < 256 && Stopwatch.GetTimestamp() < ingressDeadline && inbound.TryDequeue(out var entry);
             count++) {
            try {
                Server!.Network.ProcessMessage(entry.Message, this);
            }
            finally {
                if (entry.Buffer is not null) {
                    Server!.Network.ReleaseInbound(entry.Message);
                    Interlocked.Add(ref queuedBytes, -entry.Message.Payload.Length);
                    Interlocked.Decrement(ref queuedMessages);
                    ArrayPool<byte>.Shared.Return(entry.Buffer, clearArray: true);
                }
            }
        }
        ChunkSendBudgetRemaining = Stopwatch.Frequency * Dimension.ChunkSendBudgetMilliseconds / 1000;
        foreach (Dimension dimension in Dimensions.Values)
            dimension.Tick();

        Ticks.Record(start);
    }

    public void Dispose() {
        foreach (Dimension dimension in Dimensions.Values)
            dimension.FlushStorage();
        storage?.Dispose();
        if (ownsGenerationService)
            generationService.Dispose();
        while (inbound.TryDequeue(out var entry)) {
            if (entry.Buffer is not null)
                ArrayPool<byte>.Shared.Return(entry.Buffer, clearArray: true);
        }
    }
}
