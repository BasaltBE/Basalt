using System.Collections.Concurrent;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.NetherNet.Types;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using BasaltBE.Basalt.Worlds.Types;

namespace BasaltBE.Basalt.Worlds.Dimensions;

public sealed class Dimension : IDisposable {
    const float BroadcastRadius = 65;
    readonly Dictionary<long, Chunk?> chunks = [];
    readonly ConcurrentQueue<ChunkGenerationResult> completed = new();
    readonly ChunkGenerationService generationService;
    readonly bool ownsGenerationService;
    readonly Lock playersSync = new();
    readonly Dictionary<ulong, Entity> entities = [];
    readonly Dictionary<long, Player[]> chunkViewers = [];
    Player[] players = [];
    int chunkCount;
    int pendingRequestCount;

    public readonly string Identifier;
    public readonly IChunkGenerator Generator;

    public Dimension(string identifier, IChunkGenerator generator) {
        ArgumentException.ThrowIfNullOrEmpty(identifier);
        ArgumentNullException.ThrowIfNull(generator);
        Identifier = identifier;
        Generator = generator;
        generationService = new ChunkGenerationService(4, 1024);
        ownsGenerationService = true;
    }

    internal Dimension(string identifier, IChunkGenerator generator, ChunkGenerationService generationService)
        : this(identifier, generator, generationService, false) { }

    Dimension(string identifier, IChunkGenerator generator, ChunkGenerationService generationService, bool ownsGenerationService) {
        ArgumentException.ThrowIfNullOrEmpty(identifier);
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(generationService);
        Identifier = identifier;
        Generator = generator;
        this.generationService = generationService;
        this.ownsGenerationService = ownsGenerationService;
    }

    public int ChunkCount => chunkCount;
    public int PendingRequestCount => pendingRequestCount;

    /// <summary>Returns a stable snapshot of players who have received this chunk.</summary>
    public ReadOnlySpan<Player> GetChunkViewers(int chunkX, int chunkZ) {
        lock (playersSync)
            return chunkViewers.GetValueOrDefault(GetKey(chunkX, chunkZ)) ?? [];
    }

    /// <summary>Sends a packet to active viewers of a chunk in this dimension.</summary>
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
                // The transport disconnect event removes stale viewer entries.
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
        ArgumentNullException.ThrowIfNull(entity);
        if (entity is Player)
            throw new ArgumentException("Add players through World.AddPlayer.", nameof(entity));
        lock (playersSync) {
            if (!entities.TryAdd(entity.RuntimeId, entity))
                throw new InvalidOperationException("An entity with this runtime ID already belongs to the dimension.");
        }
    }

    public bool RemoveEntity(Entity entity) {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity is Player)
            throw new ArgumentException("Remove players through World.RemovePlayer.", nameof(entity));
        lock (playersSync) {
            return ReferenceEquals(entities.GetValueOrDefault(entity.RuntimeId), entity) &&
                entities.Remove(entity.RuntimeId);
        }
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
        long key = GetKey(chunkX, chunkZ);
        if (chunks.TryGetValue(key, out Chunk? chunk) && chunk is not null) {
            return chunk;
        }

        chunk = new Chunk(chunkX, chunkZ);
        chunks[key] = chunk;
        chunkCount++;
        return chunk;
    }

    public ChunkRequestStatus Request(int chunkX, int chunkZ) {
        long key = GetKey(chunkX, chunkZ);
        if (chunks.TryGetValue(key, out Chunk? chunk)) {
            return chunk is null ? ChunkRequestStatus.AlreadyPending : ChunkRequestStatus.AlreadyLoaded;
        }

        if (generationService.Request(this, Generator, key, chunkX, chunkZ)) {
            chunks.Add(key, null);
            pendingRequestCount++;
            return ChunkRequestStatus.Queued;
        }

        return ChunkRequestStatus.QueueFull;
    }

    public ChunkRequestStatus Request(ChunkPos position) => Request(position.X, position.Z);

    public void Broadcast(DataPacket packet, Vec3 position, Player? exclude = null) {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(position);

        float radiusSquared = BroadcastRadius * BroadcastRadius;
        foreach (Player player in Volatile.Read(ref players)) {
            if (!player.Spawned || player.Disposed || ReferenceEquals(player, exclude))
                continue;

            float x = player.Position.X - position.X;
            float y = player.Position.Y - position.Y;
            float z = player.Position.Z - position.Z;
            if (x * x + y * y + z * z <= radiusSquared)
                player.Network.SendPacket(player, packet);
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

            Player[] reduced = new Player[current.Length - 1];
            if (index > 0)
                Array.Copy(current, 0, reduced, 0, index);
            if (index < reduced.Length)
                Array.Copy(current, index + 1, reduced, index, reduced.Length - index);
            Volatile.Write(ref players, reduced);
        }
    }

    public ChunkRequestBatchResult Request(ReadOnlySpan<ChunkPos> positions) {
        int queued = 0;
        int alreadyLoaded = 0;
        int alreadyPending = 0;
        int queueFull = 0;
        foreach (ChunkPos position in positions) {
            long key = GetKey(position.X, position.Z);
            if (chunks.TryGetValue(key, out Chunk? existing)) {
                if (existing is null) {
                    alreadyPending++;
                } else {
                    alreadyLoaded++;
                }
                continue;
            }

            if (generationService.Request(this, Generator, key, position.X, position.Z)) {
                chunks.Add(key, null);
                pendingRequestCount++;
                queued++;
            } else {
                queueFull++;
            }
        }

        return new ChunkRequestBatchResult(queued, alreadyLoaded, alreadyPending, queueFull);
    }

    public int DrainGeneratedChunks() {
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
                    chunks[result.Key] = result.Chunk;
                    chunkCount++;
                    committed++;
                } else {
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

        chunk.SetPermutation(x & 15, y, z & 15, permutation, layer);
    }

    static long GetKey(int x, int z) => ((long)x << 32) | (uint)z;

    public void Dispose() {
        if (ownsGenerationService) {
            generationService.Dispose();
        }
    }
}
