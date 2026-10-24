using System.Collections.Concurrent;
using System.Diagnostics;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Events;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Basalt.Worlds.Types;
using BasaltBE.Logging;
using BasaltBE.Nbt.Types;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Worlds.Dimensions;

public sealed partial class Dimension : IDisposable {
    const float BroadcastRadius = 65;
    internal const int ChunkSendBudgetMilliseconds = 4;
    readonly Dictionary<long, Chunk?> chunks = new(ChunkKeyComparer.Instance);
    readonly ConcurrentQueue<ChunkGenerationResult> completed = new();
    readonly ChunkGenerationService generationService;
    readonly bool ownsGenerationService;
    readonly Lock playersSync = new();
    readonly Dictionary<ulong, Entity> entities = [];
    readonly Dictionary<long, Player[]> chunkViewers = new(ChunkKeyComparer.Instance);
    Player[] players = [];
    Entity[]? entitySnapshot;
    internal readonly EntitySpatialIndex EntityIndex = new();
    internal readonly ConcurrentQueue<Player> RemovedPlayers = new();
    readonly HashSet<long> simulatedChunks = new(ChunkKeyComparer.Instance);
    readonly Dictionary<long, int> simulationReferences = new(ChunkKeyComparer.Instance);
    readonly Dictionary<Player, (int X, int Z)> playerAreas = [];
    readonly List<Player> expiredAreas = [];
    readonly Dictionary<string, long[]> tickingAreas = new(StringComparer.Ordinal);
    readonly List<Entity> simulation = [];
    internal ulong TickValue;
    internal long ChunkSendBudgetRemaining;
    internal int SimulationRadius = 4;
    internal bool PrimaryStorageDimension;
    int nextPlayerTick;
    int chunkCount;
    int pendingRequestCount;

    /// <summary>Unique dimension instance identifier within its world.</summary>
    public readonly string Identifier;
    public readonly DimensionType Type;
    public readonly int RuntimeId;
    public readonly CustomDimensionDefinition? CustomDefinition;
    public GeneratorType GeneratorType => CustomDefinition?.GeneratorType ?? Type switch {
        DimensionType.Overworld => GeneratorType.Overworld,
        DimensionType.Nether => GeneratorType.Nether,
        DimensionType.TheEnd => GeneratorType.TheEnd,
        _ => GeneratorType.Undefined
    };
    public readonly IChunkGenerator Generator;
    public readonly Vec3 SpawnPosition = new() { X = 0.5f, Y = -57, Z = 0.5f };
    public Difficulty Difficulty = Difficulty.Normal;
    internal World? World;
    internal EventEmitter? Events;

    public Dimension(string identifier, IChunkGenerator generator)
        : this(identifier, GetDimensionType(identifier), generator) { }

    public Dimension(string identifier, DimensionType type, IChunkGenerator generator) {
        ArgumentException.ThrowIfNullOrEmpty(identifier);
        ArgumentNullException.ThrowIfNull(generator);
        Identifier = identifier;
        Type = type;
        RuntimeId = (int)type;
        Generator = generator;
        generationService = new ChunkGenerationService(4, 1024);
        ownsGenerationService = true;
    }

    internal Dimension(string identifier, IChunkGenerator generator, ChunkGenerationService generationService)
        : this(identifier, GetDimensionType(identifier), generator, generationService, false) { }

    internal Dimension(
        string identifier,
        DimensionType type,
        IChunkGenerator generator,
        ChunkGenerationService generationService
    ) : this(identifier, type, generator, generationService, false) { }

    Dimension(
        string identifier,
        DimensionType type,
        IChunkGenerator generator,
        ChunkGenerationService generationService,
        bool ownsGenerationService
    ) {
        ArgumentException.ThrowIfNullOrEmpty(identifier);
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(generationService);
        Identifier = identifier;
        Type = type;
        Generator = generator;
        this.generationService = generationService;
        this.ownsGenerationService = ownsGenerationService;
    }

    internal Dimension(
        string identifier,
        CustomDimensionDefinition definition,
        IChunkGenerator generator,
        ChunkGenerationService generationService
    ) {
        ArgumentException.ThrowIfNullOrEmpty(identifier);
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(generationService);
        Identifier = identifier;
        Type = DimensionType.Undefined;
        RuntimeId = definition.RuntimeId;
        CustomDefinition = definition;
        Generator = generator;
        this.generationService = generationService;
    }

    internal static DimensionType GetDimensionType(string identifier) => identifier switch {
        "minecraft:overworld" or "overworld" => DimensionType.Overworld,
        "minecraft:nether" or "nether" => DimensionType.Nether,
        "minecraft:the_end" or "minecraft:end" or "the_end" or "end" => DimensionType.TheEnd,
        _ => DimensionType.Overworld
    };

    public int ChunkCount => Volatile.Read(ref chunkCount);
    public int PendingRequestCount => Volatile.Read(ref pendingRequestCount);

    public int EntityCount {
        get {
            lock (playersSync)
                return entities.Count;
        }
    }

    public ReadOnlySpan<Player> GetPlayers() => Volatile.Read(ref players);

    public ReadOnlySpan<Entity> GetEntities(bool playersOnly = false) {
        if (playersOnly)
            return Volatile.Read(ref players);
        if (Volatile.Read(ref entitySnapshot) is { } snapshot)
            return snapshot;
        lock (playersSync) {
            entitySnapshot ??= [.. entities.Values];
            return entitySnapshot;
        }
    }

    public ReadOnlySpan<Player> GetChunkViewers(int chunkX, int chunkZ) {
        lock (playersSync)
            return chunkViewers.GetValueOrDefault(GetKey(chunkX, chunkZ)) ?? [];
    }

    public void Broadcast(DataPacket packet, int chunkX, int chunkZ, Player? exclude = null, DataPacket? additionalPacket = null) {
        ArgumentNullException.ThrowIfNull(packet);
        foreach (Player player in GetChunkViewers(chunkX, chunkZ)) {
            if (!player.Spawned || player.Disposed || ReferenceEquals(player, exclude))
                continue;
            try {
                player.Network.SendPacket(player, packet);
                if (additionalPacket is not null)
                    player.Network.SendPacket(player, additionalPacket);
            }
            catch (Exception error) when (error is PeerDisconnectedException or ObjectDisposedException) {
            }
        }
    }

    internal void AddChunkViewer(Player player, int chunkX, int chunkZ) {
        lock (playersSync) {
            long key = GetKey(chunkX, chunkZ);
            Player[] current = chunkViewers.GetValueOrDefault(key) ?? [];
            if (Array.IndexOf(current, player) >= 0)
                return;

            Player[] expanded = new Player[current.Length + 1];
            current.CopyTo(expanded, 0);
            expanded[^1] = player;
            chunkViewers[key] = expanded;
        }
    }

    internal void RemoveChunkViewer(Player player, int chunkX, int chunkZ) {
        lock (playersSync) {
            long key = GetKey(chunkX, chunkZ);
            if (!chunkViewers.TryGetValue(key, out Player[]? current))
                return;
            int index = Array.IndexOf(current, player);
            if (index < 0)
                return;
            if (current.Length == 1) {
                chunkViewers.Remove(key);
                return;
            }

            Player[] reduced = new Player[current.Length - 1];
            current.AsSpan(0, index).CopyTo(reduced);
            current.AsSpan(index + 1).CopyTo(reduced.AsSpan(index));
            chunkViewers[key] = reduced;
        }
    }

    public Entity? GetEntity(ulong runtimeId) {
        lock (playersSync)
            return entities.GetValueOrDefault(runtimeId);
    }

    public void AddEntity(Entity entity) {
        World?.CheckOwnership();
        ArgumentNullException.ThrowIfNull(entity);
        if (entity is Player)
            throw new ArgumentException("Add players through World.AddPlayer.", nameof(entity));
        lock (playersSync) {
            if (entity.SpawnedDimension is not null)
                throw new InvalidOperationException("Entity already belongs to a dimension.");
            if (entity.SpatialSlot >= 0)
                throw new InvalidOperationException("Wait for the current simulation step to finish before respawning this entity.");
            if (!entities.TryAdd(entity.RuntimeId, entity))
                throw new InvalidOperationException("An entity with this runtime ID already belongs to the dimension.");
            Volatile.Write(ref entitySnapshot, null);
            EntityIndex.Update(entity);
            entity.SpawnedDimension = this;
        }
        entity.OnSpawn();
    }

    public bool RemoveEntity(Entity entity) {
        World?.CheckOwnership();
        ArgumentNullException.ThrowIfNull(entity);
        if (entity is Player)
            throw new ArgumentException("Remove players through World.RemovePlayer.", nameof(entity));
        lock (playersSync) {
            if (!ReferenceEquals(entities.GetValueOrDefault(entity.RuntimeId), entity) || !entities.Remove(entity.RuntimeId))
                return false;
            Volatile.Write(ref entitySnapshot, null);
            EntityIndex.Remove(entity);
            entity.SpawnedDimension = null;
        }
        entity.OnDespawn();
        return true;
    }

    public bool Empty {
        get {
            foreach (Chunk? chunk in chunks.Values) {
                if (chunk is not null && !chunk.Empty) {
                    return false;
                }
            }

            return true;
        }
    }

    public Chunk? GetChunk(int chunkX, int chunkZ) =>
        chunks.GetValueOrDefault(GetKey(chunkX, chunkZ));

    public Chunk GetOrCreateChunk(int chunkX, int chunkZ) {
        World?.CheckOwnership();
        long key = GetKey(chunkX, chunkZ);
        if (chunks.TryGetValue(key, out Chunk? chunk) && chunk is not null) {
            return chunk;
        }

        chunk = new Chunk(chunkX, chunkZ);
        chunk.Dimension = this;
        chunks[key] = chunk;
        chunkCount++;
        return chunk;
    }

    public ChunkRequestStatus Request(int chunkX, int chunkZ) {
        World?.CheckOwnership();
        long key = GetKey(chunkX, chunkZ);
        if (chunks.TryGetValue(key, out Chunk? chunk)) {
            return chunk is null ? ChunkRequestStatus.AlreadyPending : ChunkRequestStatus.AlreadyLoaded;
        }

        if (QueueChunk(chunkX, chunkZ, key)) {
            return ChunkRequestStatus.Queued;
        }

        return ChunkRequestStatus.QueueFull;
    }

    public ChunkRequestStatus Request(ChunkPos position) => Request(position.X, position.Z);

    public void Broadcast(DataPacket packet, Vec3 position, Player? exclude = null, float radius = BroadcastRadius) {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(position);
        if (!float.IsFinite(radius) || radius < 0)
            throw new ArgumentOutOfRangeException(nameof(radius));

        float radiusSquared = radius * radius;
        foreach (Player player in Volatile.Read(ref players)) {
            if (!player.Spawned || player.Disposed || ReferenceEquals(player, exclude))
                continue;

            float x = player.Position.X - position.X;
            float y = player.Position.Y - position.Y;
            float z = player.Position.Z - position.Z;
            if (x * x + y * y + z * z <= radiusSquared) {
                try {
                    player.Network.SendPacket(player, packet);
                }
                catch (Exception error) when (error is PeerDisconnectedException or ObjectDisposedException) {
                }
            }
        }
    }

    public void Broadcast(DataPacket packet, Entity entity, Player? exclude = null) {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(entity);
        foreach (Player viewer in GetPlayers()) {
            if (!viewer.Spawned || viewer.Disposed || ReferenceEquals(viewer, exclude) ||
                !ReferenceEquals(viewer, entity) && viewer.GetTrait<PlayerChunkRenderingTrait>()?.Viewing(entity) != true)
                continue;
            try {
                viewer.Network.SendPacket(viewer, packet);
            }
            catch (Exception error) when (error is PeerDisconnectedException or ObjectDisposedException) {
            }
        }
    }

    internal void Broadcast(DataPacket packet, Player? exclude) {
        foreach (Player player in Volatile.Read(ref players)) {
            if (!player.Disposed && !ReferenceEquals(player, exclude))
                player.Network.SendPacket(player, packet);
        }
    }

    internal void AddPlayer(Player player) {
        lock (playersSync) {
            if (!entities.TryAdd(player.RuntimeId, player))
                throw new InvalidOperationException("An entity with this runtime ID already belongs to the dimension.");
            Volatile.Write(ref entitySnapshot, null);
            EntityIndex.Update(player);
            player.SpawnedDimension = this;
            Player[] current = players;
            Player[] expanded = new Player[current.Length + 1];
            current.CopyTo(expanded, 0);
            expanded[^1] = player;
            Volatile.Write(ref players, expanded);
        }
    }

    internal void RemovePlayer(Player player) {
        player.GetTrait<PlayerChunkRenderingTrait>()?.OnDespawn();
        lock (playersSync) {
            Player[] current = players;
            int index = Array.IndexOf(current, player);
            if (index < 0)
                return;
            entities.Remove(player.RuntimeId);
            player.SpawnedDimension = null;
            Volatile.Write(ref entitySnapshot, null);
            EntityIndex.Remove(player);

            Player[] reduced = new Player[current.Length - 1];
            if (index > 0)
                Array.Copy(current, 0, reduced, 0, index);
            if (index < reduced.Length)
                Array.Copy(current, index + 1, reduced, index, reduced.Length - index);
            Volatile.Write(ref players, reduced);
        }
    }

    public ChunkRequestBatchResult Request(ReadOnlySpan<ChunkPos> positions) {
        World?.CheckOwnership();
        int queued = 0;
        int alreadyLoaded = 0;
        int alreadyPending = 0;
        int queueFull = 0;
        foreach (ChunkPos position in positions) {
            long key = GetKey(position.X, position.Z);
            if (chunks.TryGetValue(key, out Chunk? existing)) {
                if (existing is null) {
                    alreadyPending++;
                }
                else {
                    alreadyLoaded++;
                }
                continue;
            }

            if (QueueChunk(position.X, position.Z, key)) {
                queued++;
            }
            else {
                queueFull++;
            }
        }

        return new ChunkRequestBatchResult(queued, alreadyLoaded, alreadyPending, queueFull);
    }

    public int DrainGeneratedChunks() {
        World?.CheckOwnership();
        int committed = 0;
        Exception? error = null;
        while (completed.TryDequeue(out ChunkGenerationResult result)) {
            pendingRequestCount--;
            generationService.ReleaseRequest();
            if (result.Error is not null) {
                if (chunks.TryGetValue(result.Key, out Chunk? pendingChunk) && pendingChunk is null) {
                    chunks.Remove(result.Key);
                }
                error ??= result.Error;
                continue;
            }

            if (chunks.TryGetValue(result.Key, out Chunk? current) && current is null) {
                if (result.Chunk is not null) {
                    result.Chunk.Dimension = this;
                    chunks[result.Key] = result.Chunk;
                    chunkCount++;
                    if (World?.Storage is not null)
                        result.Chunk.MarkDirty();
                    committed++;
                }
                else {
                    chunks.Remove(result.Key);
                }
            }
        }

        if (error is not null) {
            throw new InvalidOperationException("Chunk generation failed.", error);
        }

        return committed;
    }

    internal void CompleteGeneration(long key, Chunk? chunk, Exception? error) =>
        completed.Enqueue(new ChunkGenerationResult(key, chunk, error));

    public BlockPermutation GetPermutation(int x, int y, int z, int layer = 0) {
        if ((uint)layer > 1) {
            throw new ArgumentOutOfRangeException(nameof(layer), "A subchunk supports two block layers.");
        }

        int chunkX = x >> 4;
        int chunkZ = z >> 4;
        return chunks.TryGetValue(GetKey(chunkX, chunkZ), out Chunk? chunk) && chunk is not null
            ? chunk.GetPermutation(x & 15, y, z & 15, layer)
            : BlockStorage.Air;
    }

    public void SetPermutation(int x, int y, int z, BlockPermutation permutation, int layer = 0) {
        World?.CheckOwnership();
        ArgumentNullException.ThrowIfNull(permutation);
        if ((uint)layer > 1) {
            throw new ArgumentOutOfRangeException(nameof(layer), "A subchunk supports two block layers.");
        }

        int chunkX = x >> 4;
        int chunkZ = z >> 4;
        if (!chunks.TryGetValue(GetKey(chunkX, chunkZ), out Chunk? chunk) || chunk is null) {
            if (permutation == BlockStorage.Air) {
                return;
            }

            chunk = GetOrCreateChunk(chunkX, chunkZ);
        }

        if (layer == 0 && chunk.GetPermutation(x & 15, y, z & 15, layer) != permutation)
            RemoveBlockActor(x, y, z);
        chunk.SetPermutation(x & 15, y, z & 15, permutation, layer);
    }

    internal void UpdateNeighbors(int x, int y, int z) {
        UpdateNeighbor(x + 1, y, z);
        UpdateNeighbor(x - 1, y, z);
        UpdateNeighbor(x, y, z + 1);
        UpdateNeighbor(x, y, z - 1);
        UpdateNeighbor(x, y + 1, z);
        UpdateNeighbor(x, y - 1, z);
    }

    void UpdateNeighbor(int x, int y, int z) {
        if (!BlockPalette.OnNeighborUpdate(this, x, y, z))
            return;

        BlockPermutation permutation = GetPermutation(x, y, z);
        Broadcast(new UpdateBlockPacket {
            Position = new BlockPos { X = x, Y = y, Z = z },
            BlockRuntimeId = unchecked((uint)permutation.Hash),
            Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network
        }, x >> 4, z >> 4);
    }

    internal BlockActor? GetBlockActor(Chunk chunk, int x, int y, int z) {
        if (chunk.GetRuntimeActor(x, y, z) is { } actor)
            return actor;
        if (chunk.GetBlockNbt(x, y, z) is not { } nbt)
            return null;

        BlockPermutation permutation = chunk.GetPermutation(x, y, z);
        BlockActor? loaded = World?.Server?.BlockActors.Create(
            permutation.Identifier,
            new BlockActorContext(
                this,
                new BlockPos { X = chunk.X * 16 + x, Y = y, Z = chunk.Z * 16 + z },
                permutation.Identifier,
                permutation,
                nbt
            )
        );
        if (loaded is null)
            return null;

        loaded.OnRead(nbt);
        chunk.SetRuntimeActor(x, y, z, loaded);
        return loaded;
    }

    internal BlockActor? PlaceBlockActor(int x, int y, int z, string blockIdentifier, ItemStack? item, byte face) {
        Chunk chunk = GetOrCreateChunk(x >> 4, z >> 4);
        BlockActorContext context = new(
            this,
            new BlockPos { X = x, Y = y, Z = z },
            blockIdentifier,
            GetPermutation(x, y, z),
            new CompoundTag()
        );
        BlockActor? actor = World?.Server?.BlockActors.Create(blockIdentifier, context);
        if (actor is null)
            return null;

        actor.Nbt.Add("id", new StringTag { Value = actor.ActorIdentifier });
        actor.Nbt.Add("x", new IntTag { Value = x });
        actor.Nbt.Add("y", new IntTag { Value = y });
        actor.Nbt.Add("z", new IntTag { Value = z });
        if (item?.Nbt is { } itemNbt) {
            Logger.Scoped("Network").Info(
                $"Placing {blockIdentifier} with item tags ({string.Join(", ", itemNbt.Tags.Keys)})."
            );
        }
        actor.OnPlace(item, face);
        if (item?.Nbt is not null) {
            CompoundTag itemData = actor.CreateItemData();
            Logger.Scoped("Network").Info(
                $"Placed {blockIdentifier} actor data tags ({string.Join(", ", itemData.Tags.Keys)})."
            );
        }
        chunk.SetBlockNbt(x & 15, y, z & 15, actor.Nbt);
        chunk.SetRuntimeActor(x & 15, y, z & 15, actor);
        return actor;
    }

    internal void RemoveBlockActor(int x, int y, int z) {
        Chunk? chunk = GetChunk(x >> 4, z >> 4);
        if (chunk is null)
            return;

        BlockActor? actor = GetBlockActor(chunk, x & 15, y, z & 15);
        actor?.OnRemove();
        chunk.SetBlockNbt(x & 15, y, z & 15, null);
    }

    public void AddTickingArea(string identifier, ReadOnlySpan<ChunkPos> positions) {
        World?.CheckOwnership();
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        if (positions.Length > 4096)
            throw new ArgumentOutOfRangeException(nameof(positions));

        long[] keys = new long[positions.Length];
        for (int index = 0; index < positions.Length; index++)
            keys[index] = GetKey(positions[index].X, positions[index].Z);

        tickingAreas.Add(identifier, keys);

        foreach (long key in keys)
            ChangeSimulation(key, 1);
    }

    public bool RemoveTickingArea(string identifier) {
        World?.CheckOwnership();

        if (!tickingAreas.Remove(identifier, out long[]? keys))
            return false;

        foreach (long key in keys)
            ChangeSimulation(key, -1);

        return true;
    }

    void ChangeSimulation(long key, int change) {
        int count = simulationReferences.GetValueOrDefault(key) + change;

        if (count == 0) {
            simulationReferences.Remove(key);
            simulatedChunks.Remove(key);
        }
        else {
            simulationReferences[key] = count;
            simulatedChunks.Add(key);
        }
    }

    void MoveSimulationArea((int X, int Z)? previous, (int X, int Z)? current) {
        if (previous == current)
            return;

        if (previous is { } old) {
            for (int x = old.X - SimulationRadius; x <= old.X + SimulationRadius; x++) {
                for (int z = old.Z - SimulationRadius; z <= old.Z + SimulationRadius; z++) {
                    if (current is not { } next ||
                        Math.Abs((long)x - next.X) > SimulationRadius ||
                        Math.Abs((long)z - next.Z) > SimulationRadius)
                        ChangeSimulation(GetKey(x, z), -1);
                }
            }
        }

        if (current is { } added) {
            for (int x = added.X - SimulationRadius; x <= added.X + SimulationRadius; x++) {
                for (int z = added.Z - SimulationRadius; z <= added.Z + SimulationRadius; z++) {
                    if (previous is not { } prior ||
                        Math.Abs((long)x - prior.X) > SimulationRadius ||
                        Math.Abs((long)z - prior.Z) > SimulationRadius)
                        ChangeSimulation(GetKey(x, z), 1);
                }
            }
        }
    }

    public void Tick() {
        World?.CheckOwnership();

        if (World is null)
            ChunkSendBudgetRemaining = Stopwatch.Frequency * ChunkSendBudgetMilliseconds / 1000;

        TickValue++;
        TickStorageCompletions();
        DrainGeneratedChunks();

        foreach ((Player player, var area) in playerAreas) {
            if (!player.Spawned || player.Disposed || player.SpawnedDimension != this)
                expiredAreas.Add(player);
        }

        foreach (Player player in expiredAreas) {
            MoveSimulationArea(playerAreas[player], null);
            playerAreas.Remove(player);
        }
        expiredAreas.Clear();

        foreach (Player player in GetPlayers()) {
            if (!player.Spawned || player.Disposed)
                continue;

            var current = (
                X: (int)MathF.Floor(player.Position.X) >> 4,
                Z: (int)MathF.Floor(player.Position.Z) >> 4
            );
            (int X, int Z)? previous = playerAreas.TryGetValue(player, out var area) ? area : null;
            MoveSimulationArea(previous, current);
            playerAreas[player] = current;
        }

        EntityIndex.Collect(simulatedChunks, simulation);
        var server = World?.Server;
        foreach (long key in simulatedChunks) {
            if (!chunks.TryGetValue(key, out Chunk? chunk))
                Request((int)(key >> 32), (int)key);
            else if (chunk?.blockActors is { } actors && server is not null) {
                foreach ((int X, int Y, int Z) position in actors.Keys) {
                    BlockPermutation permutation = chunk.GetPermutation(position.X, position.Y, position.Z);
                    if (server.BlockActors.Ticks(permutation.Identifier))
                        GetBlockActor(chunk, position.X, position.Y, position.Z)?.Tick();
                }
            }
        }

        foreach (Entity entity in simulation) {
            if (entity is Player || entity.SpawnedDimension != this)
                continue;

            if (GetChunk((int)(entity.SpatialChunk >> 32), (int)entity.SpatialChunk) is null) {
                Request((int)(entity.SpatialChunk >> 32), (int)entity.SpatialChunk);
                continue;
            }

            if (entity.Dead) {
                if (++entity.SimulationAge >= entity.DeathDespawnTick)
                    RemoveEntity(entity);
                continue;
            }

            EntityIndex.Deferred = true;

            try {
                entity.Tick();
            }
            finally {
                EntityIndex.Flush();
            }

            if (entity.SpawnedDimension == this)
                EntityIndex.Update(entity);
        }
        simulation.Clear();
        World?.Server?.Network.Tick(this);

        ReadOnlySpan<Player> currentPlayers = GetPlayers();
        if (!currentPlayers.IsEmpty) {
            int startPlayer = nextPlayerTick % currentPlayers.Length;
            for (int offset = 0; offset < currentPlayers.Length; offset++) {
                Player player = currentPlayers[(startPlayer + offset) % currentPlayers.Length];
                if (player.Disposed)
                    continue;

                try {
                    player.Tick();
                    EntityIndex.Update(player);
                }
                catch (Exception error) when (error is PeerDisconnectedException or ObjectDisposedException) {
                    player.Network.RemovePlayer(player.Peer);
                }
            }

            nextPlayerTick = (startPlayer + 1) % currentPlayers.Length;

            bool sentChunk;
            do {
                sentChunk = false;
                for (int offset = 0; offset < currentPlayers.Length; offset++) {
                    long budgetRemaining = World?.ChunkSendBudgetRemaining ?? ChunkSendBudgetRemaining;
                    if (budgetRemaining <= 0)
                        break;

                    Player player = currentPlayers[(startPlayer + offset) % currentPlayers.Length];
                    if (player.Disposed)
                        continue;

                    try {
                        if (player.GetTrait<PlayerChunkRenderingTrait>()?.SendReadyChunk() == true)
                            sentChunk = true;
                    }
                    catch (Exception error) when (error is PeerDisconnectedException or ObjectDisposedException) {
                        player.Network.RemovePlayer(player.Peer);
                    }
                }
            } while (sentChunk && (World?.ChunkSendBudgetRemaining ?? ChunkSendBudgetRemaining) > 0);
        }

        TickStorageSaves();
    }

    static long GetKey(int x, int z) => ((long)x << 32) | (uint)z;

    public void Dispose() {
        if (ownsGenerationService) {
            generationService.Dispose();
        }
    }
}
