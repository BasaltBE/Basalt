namespace Basalt.Core.Player.Traits;

using System.Collections.Concurrent;
using System.Diagnostics;
using Basalt.Core.Blocks;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Traits;
using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Profiling;
using Basalt.Core.Tasks;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;
using Entity = Basalt.Core.Entities.Entity;
using Basalt.Core.Entities;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public sealed class PlayerChunkRenderingTrait : PlayerTrait {
    private const float EntityVisibilityRadiusSquared = 64f * 64f;
    private const int EntityVisibilityRadiusChunks = 4;

    public new static string Identifier => "chunk_rendering";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];

    private readonly Lock _lock = new();
    private readonly HashSet<long> _loadedChunks = [];
    private readonly HashSet<long> _requestedChunks = [];
    private readonly Queue<(int X, int Z)> _deferredChunks = [];
    private readonly Queue<(Dimension Dimension, ChunkColumn Chunk)> _readyChunks = [];
    private readonly ConcurrentQueue<SerializedChunk> _serializedChunks = new();
    private readonly HashSet<long> _serializingChunks = [];
    private readonly List<long> _unloadBuffer = [];
    private readonly List<Packet> _sendBuffer = [];
    private readonly List<(long Hash, int X, int Z)> _sentChunkBuffer = [];
    private readonly HashSet<long> _visibleChunks = [];
    private readonly HashSet<long> _visibilityChunkBuffer = [];
    private readonly List<long> _visibilityRemoveBuffer = [];

    /// <summary>
    /// Maps entity runtime ID to its unique ID for all entities currently visible to this player.
    /// </summary>
    public Dictionary<ulong, long> VisibleActorIds { get; } = [];

    public int ChunkX { get; private set; } = int.MinValue;
    public int ChunkZ { get; private set; } = int.MinValue;

    private int _publisherChunkX = int.MinValue;
    private int _publisherChunkZ = int.MinValue;
    private bool _publisherSent;

    private int _ringRadius;
    private int _ringIndex;
    private int _chunkRequestVersion;
    private long _chunkLoadStartTimestamp;
    private double _firstChunkMilliseconds;

    private bool _started;

    public int ViewDistance { get; private set; } = 16;
    public int LoadedChunkCount => _loadedChunks.Count;
    public double FirstChunkMilliseconds => _firstChunkMilliseconds;

    public PlayerChunkRenderingTrait(Entity entity) : base(entity) {
    }

    public void SetViewDistance(int distance) {
        ViewDistance = Math.Clamp(distance, 1, 120);
    }

    public void ApplyViewDistance(int distance) {
        lock (_lock) {
            int viewDistance = Math.Clamp(distance, 1, 120);
            if (ViewDistance == viewDistance) {
                return;
            }

            ViewDistance = viewDistance;
            ResetRingScan();
            ResetChunkRequests();

            if (!_started || Player.Dimension is null) {
                return;
            }

            UpdateTrackedChunkPosition();
            UnloadChunks(Player.Dimension, clearClient: true);
            _publisherSent = false;
        }
    }

    public void StartChunkLoad() {
        lock (_lock) {
            _started = true;
            _loadedChunks.Clear();
            _chunkLoadStartTimestamp = Stopwatch.GetTimestamp();
            _firstChunkMilliseconds = 0;
            ResetChunkRequests();
            UpdateTrackedChunkPosition();
            ResetRingScan();
            _publisherSent = false;

            if (Player.Dimension is not null) {
                SendChunks(Player.Dimension);
                UpdateVisibleChunks(Player.Dimension);
            }
        }
    }

    public override void OnSpawn(EntitySpawnOptions details) {
        UpdateTrackedChunkPosition();
    }

    public override void OnTeleport(EntityTeleportOptions details) {
        Dimension? dimension = Player.Dimension;
        lock (_lock) {
            if (dimension is null) {
                return;
            }

            if (details.ChangedDimension) {
                HideAllVisibleEntities();
                _visibleChunks.Clear();
                UnloadChunks(dimension, clearClient: true, force: true);
                _loadedChunks.Clear();
                ResetChunkRequests();
                VisibleActorIds.Clear();
                UpdateTrackedChunkPosition();
                ResetRingScan();
                return;
            }

            int chunkX = WorldToChunk(details.To.X);
            int chunkZ = WorldToChunk(details.To.Z);
            if (UpdateChunkPosition(chunkX, chunkZ)) {
                UnloadChunks(dimension, clearClient: true);
                _publisherSent = false;
                UpdateVisibleChunks(dimension);
            }
        }

        dimension.UpdatePlayerVisibility(Player, details.From);
    }

    public override void OnMove(EntityMoveOptions details) {
        if (!_started || !Player.IsAlive || Player.Dimension is null) {
            return;
        }

        Dimension dimension = Player.Dimension;
        lock (_lock) {
            int chunkX = WorldToChunk(details.To.X);
            int chunkZ = WorldToChunk(details.To.Z);

            if (UpdateChunkPosition(chunkX, chunkZ)) {
                UnloadChunks(dimension, clearClient: true);
                UpdateVisibleChunks(dimension);
                _publisherSent = false;
            }
        }

        if (details.From.X != details.To.X ||
            details.From.Y != details.To.Y ||
            details.From.Z != details.To.Z) {
            dimension.UpdatePlayerVisibility(Player, details.From);
        }
    }

    public override void OnTick(TraitOnTickDetails details) {
        if (!_started || !Player.IsAlive || Player.Dimension is null) {
            return;
        }

        using var __zone = Profiler.Enabled ? Profiler.BeginZone("PlayerChunkRendering.OnTick") : default;
        lock (_lock) {
            Dimension dimension = Player.Dimension;
            int chunkX = WorldToChunk(Player.Location.X);
            int chunkZ = WorldToChunk(Player.Location.Z);

            bool changedChunk = UpdateChunkPosition(chunkX, chunkZ);
            UnloadChunks(dimension, clearClient: true);
            if (changedChunk) {
                _publisherSent = false;
                UpdateVisibleChunks(dimension);
            }

            SendChunks(dimension);
        }
    }

    public override void OnDespawn(EntityDespawnOptions details) {
        Clear();
    }

    public override void OnRemove() {
        Clear();
    }

    public override EntityTrait Clone(Entity entity) {
        PlayerChunkRenderingTrait trait = new(entity);
        trait.SetViewDistance(ViewDistance);
        return trait;
    }

    private void SendChunks(Dimension dimension) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("PlayerChunkRendering.SendChunks") : default;
        int chunksPerTick = Math.Max(dimension.World?.Server?.Properties.ChunksPerTick ?? 64, 1);
        _sendBuffer.Clear();
        _sentChunkBuffer.Clear();

        SendReadyChunks(dimension, chunksPerTick);
        SendSerializedChunks(dimension, chunksPerTick);

        int requestLimit = chunksPerTick - _requestedChunks.Count;
        if (requestLimit > 0) {
            Span<(int X, int Z)> requests = stackalloc (int X, int Z)[chunksPerTick];
            int requestCount = 0;

            while (requestCount < requestLimit && _deferredChunks.Count > 0) {
                (int x, int z) = _deferredChunks.Dequeue();
                long hash = HashChunk(x, z);
                if (_loadedChunks.Contains(hash) || !_requestedChunks.Add(hash)) {
                    continue;
                }

                requests[requestCount++] = (x, z);
            }

            while (requestCount < requestLimit && NextRingPosition(out int x, out int z)) {
                long hash = HashChunk(x, z);
                if (_loadedChunks.Contains(hash) || !_requestedChunks.Add(hash)) {
                    continue;
                }

                requests[requestCount++] = (x, z);
            }

            if (requestCount > 0) {
                int requestVersion = _chunkRequestVersion;
                int accepted = dimension.RequestChunks(requests[..requestCount], chunk => {
                    lock (_lock) {
                        if (!_started ||
                            requestVersion != _chunkRequestVersion ||
                            !ReferenceEquals(Player.Dimension, dimension) ||
                            !_requestedChunks.Contains(chunk.Hash)) {
                            return;
                        }

                        if (!ChunkInRange(chunk.X, chunk.Z)) {
                            _requestedChunks.Remove(chunk.Hash);
                            return;
                        }

                        dimension.AddChunkViewer(chunk.X, chunk.Z);
                        _readyChunks.Enqueue((dimension, chunk));
                    }
                });

                for (int i = accepted; i < requestCount; i++) {
                    (int x, int z) = requests[i];
                    _requestedChunks.Remove(HashChunk(x, z));
                    _deferredChunks.Enqueue((x, z));
                }
            }
        }

        if (_sendBuffer.Count == 0) {
            return;
        }

        bool sendPublisher = !_publisherSent && Player.Location.Y >= 0;
        if (sendPublisher) {
            _sendBuffer.Add(CreateChunkPublisherPacket());
        }

        Player.Send([.. _sendBuffer]);

        if (_firstChunkMilliseconds == 0 && _chunkLoadStartTimestamp != 0 && _sentChunkBuffer.Count > 0) {
            _firstChunkMilliseconds = Stopwatch.GetElapsedTime(_chunkLoadStartTimestamp).TotalMilliseconds;
        }

        foreach ((long hash, int x, int z) in _sentChunkBuffer) {
            if (!_loadedChunks.Add(hash)) {
                continue;
            }

            SendChunkChestVisualUpdates(dimension, x, z);
        }

        if (sendPublisher) {
            _publisherChunkX = ChunkX;
            _publisherChunkZ = ChunkZ;
            _publisherSent = true;
        }
    }

    private void SendReadyChunks(Dimension dimension, int chunksPerTick) {
        int available = chunksPerTick - _sendBuffer.Count - _serializingChunks.Count;
        while (available > 0 && _readyChunks.Count > 0) {
            (Dimension requestedDimension, ChunkColumn chunk) = _readyChunks.Dequeue();
            _requestedChunks.Remove(chunk.Hash);

            if (!ReferenceEquals(requestedDimension, dimension) ||
                _loadedChunks.Contains(chunk.Hash) ||
                !ChunkInRange(chunk.X, chunk.Z)) {
                requestedDimension.RemoveChunkViewer(chunk.X, chunk.Z);
                continue;
            }

            ChunkColumn snapshot = chunk.CreatePersistenceSnapshot();
            _serializingChunks.Add(chunk.Hash);
            available--;

            int requestVersion = _chunkRequestVersion;
            TaskScheduler? scheduler = dimension.World?.Server?.Scheduler;
            if (scheduler is null) {
                try {
                    CompleteChunkSerialization(
                        requestedDimension,
                        requestVersion,
                        snapshot,
                        ChunkColumn.Serialize(snapshot),
                        null);
                }
                catch (Exception exception) {
                    CompleteChunkSerialization(requestedDimension, requestVersion, snapshot, null, exception);
                }
                continue;
            }

            ChunkSerializationTask task = new(
                snapshot,
                (serializedChunk, payload, error) => CompleteChunkSerialization(
                    requestedDimension,
                    requestVersion,
                    serializedChunk,
                    payload,
                    error)) {
                CompletionMailbox = requestedDimension.Mailbox
            };
            scheduler.Schedule(task);
        }
    }

    private void SendSerializedChunks(Dimension dimension, int chunksPerTick) {
        while (_sendBuffer.Count < chunksPerTick && _serializedChunks.TryDequeue(out SerializedChunk serialized)) {
            bool currentRequest = serialized.Version == _chunkRequestVersion;
            if (currentRequest) {
                _serializingChunks.Remove(serialized.Chunk.Hash);
            }

            if (!currentRequest ||
                !ReferenceEquals(serialized.Dimension, dimension) ||
                !_started ||
                !ChunkInRange(serialized.Chunk.X, serialized.Chunk.Z)) {
                if (currentRequest) {
                    serialized.Dimension.RemoveChunkViewer(serialized.Chunk.X, serialized.Chunk.Z);
                }
                continue;
            }

            if (serialized.Error is not null || serialized.Payload is null) {
                serialized.Dimension.RemoveChunkViewer(serialized.Chunk.X, serialized.Chunk.Z);
                Logger.Err($"Failed to serialize chunk {serialized.Chunk.X}, {serialized.Chunk.Z}: {serialized.Error?.Message ?? "No payload was produced."}");
                continue;
            }

            _sendBuffer.Add(new LevelChunkPacket {
                ChunkPosition = new ChunkPos {
                    X = serialized.Chunk.X,
                    Z = serialized.Chunk.Z
                },
                DimensionId = (int)serialized.Chunk.Type,
                SubChunksCount = (uint)serialized.Chunk.GetSubChunkSendCount(),
                ClientRequestSubChunkLimit = null,
                CacheEnabled = false,
                CacheMetadata = [],
                RawPayload = serialized.Payload
            });

            _sentChunkBuffer.Add((serialized.Chunk.Hash, serialized.Chunk.X, serialized.Chunk.Z));
        }
    }

    private void CompleteChunkSerialization(
        Dimension dimension,
        int version,
        ChunkColumn chunk,
        byte[]? payload,
        Exception? error) {
        if (version != _chunkRequestVersion) {
            if (!_requestedChunks.Contains(chunk.Hash) && !_loadedChunks.Contains(chunk.Hash)) {
                dimension.RemoveChunkViewer(chunk.X, chunk.Z);
            }

            return;
        }

        _serializingChunks.Remove(chunk.Hash);
        _serializedChunks.Enqueue(new SerializedChunk(dimension, version, chunk, payload, error));
    }

    private void UnloadChunks(Dimension dimension, bool clearClient, bool force = false) {
        if (_loadedChunks.Count == 0) {
            return;
        }

        _unloadBuffer.Clear();

        foreach (long hash in _loadedChunks) {
            UnhashChunk(hash, out int x, out int z);

            if (!force && ChunkInRange(x, z)) {
                continue;
            }

            if (clearClient) {
                Player.Send(new LevelChunkPacket {
                    ChunkPosition = new ChunkPos {
                        X = x,
                        Z = z
                    },
                    DimensionId = (int)dimension.Type,
                    SubChunksCount = 0,
                    ClientRequestSubChunkLimit = null,
                    CacheEnabled = false,
                    CacheMetadata = [],
                    RawPayload = []
                });
            }

            dimension.RemoveChunkViewer(x, z);

            if (!dimension.HasChunkViewers(x, z)) {
                dimension.UnloadChunk(x, z);
            }

            _unloadBuffer.Add(hash);
        }

        for (int i = 0; i < _unloadBuffer.Count; i++) {
            _loadedChunks.Remove(_unloadBuffer[i]);
        }
    }

    private bool UpdateChunkPosition(int chunkX, int chunkZ) {
        if (chunkX == ChunkX && chunkZ == ChunkZ) {
            return false;
        }

        ChunkX = chunkX;
        ChunkZ = chunkZ;
        ResetRingScan();
        return true;
    }

    private void ResetRingScan() {
        _ringRadius = 0;
        _ringIndex = 0;
    }

    private void ResetChunkRequests() {
        _chunkRequestVersion++;
        _requestedChunks.Clear();
        _deferredChunks.Clear();

        while (_readyChunks.TryDequeue(out (Dimension Dimension, ChunkColumn Chunk) ready)) {
            ready.Dimension.RemoveChunkViewer(ready.Chunk.X, ready.Chunk.Z);
        }

        while (_serializedChunks.TryDequeue(out SerializedChunk serialized)) {
            serialized.Dimension.RemoveChunkViewer(serialized.Chunk.X, serialized.Chunk.Z);
        }

        _serializingChunks.Clear();
    }


    private bool NextRingPosition(out int x, out int z) {
        while (_ringRadius <= ViewDistance) {
            if (_ringRadius == 0) {
                _ringRadius = 1;
                _ringIndex = 0;
                x = ChunkX;
                z = ChunkZ;
                return true;
            }

            int r = _ringRadius;
            int perimeterLength = 8 * r;

            if (_ringIndex >= perimeterLength) {
                _ringRadius++;
                _ringIndex = 0;
                continue;
            }

            int sideLength = 2 * r;
            int index = _ringIndex++;
            int offsetX;
            int offsetZ;

            if (index < sideLength) {
                offsetX = -r + index;
                offsetZ = -r;
            }
            else if (index < sideLength * 2) {
                int i = index - sideLength;
                offsetX = r;
                offsetZ = -r + i;
            }
            else if (index < sideLength * 3) {
                int i = index - (sideLength * 2);
                offsetX = r - i;
                offsetZ = r;
            }
            else {
                int i = index - (sideLength * 3);
                offsetX = -r;
                offsetZ = r - i;
            }

            x = ChunkX + offsetX;
            z = ChunkZ + offsetZ;
            return true;
        }

        x = 0;
        z = 0;
        return false;
    }

    private void Clear() {
        lock (_lock) {
            HideAllVisibleEntities();

            if (Player.Dimension is not null) {
                UnloadChunks(Player.Dimension, clearClient: false, force: true);
            }

            _loadedChunks.Clear();
            ResetChunkRequests();
            VisibleActorIds.Clear();
            _visibleChunks.Clear();
            _started = false;
            ChunkX = int.MinValue;
            ChunkZ = int.MinValue;
            _publisherChunkX = int.MinValue;
            _publisherChunkZ = int.MinValue;
            ResetRingScan();
        }
    }

    private void UpdateTrackedChunkPosition() {
        ChunkX = WorldToChunk(Player.Location.X);
        ChunkZ = WorldToChunk(Player.Location.Z);

        if (_publisherChunkX == int.MinValue) {
            _publisherChunkX = ChunkX;
            _publisherChunkZ = ChunkZ;
        }
    }

    private void SendPublisherUpdate() {
        if (_loadedChunks.Count == 0 || Player.Location.Y < 0) {
            return;
        }

        Player.Send(CreateChunkPublisherPacket());
        _publisherChunkX = ChunkX;
        _publisherChunkZ = ChunkZ;
        _publisherSent = true;
    }

    private NetworkChunkPublisherUpdatePacket CreateChunkPublisherPacket() {
        return new NetworkChunkPublisherUpdatePacket {
            Position = new BlockPos {
                X = (int)MathF.Floor(Player.Location.X),
                Y = (int)MathF.Floor(Player.Location.Y),
                Z = (int)MathF.Floor(Player.Location.Z)
            },
            Radius = (uint)ChunkViewMath.PublisherRadiusBlocks(ViewDistance),
            ServerBuiltChunks = []
        };
    }

    private static int WorldToChunk(float coordinate) {
        return (int)MathF.Floor(coordinate) >> 4;
    }

    private static long HashChunk(int x, int z) {
        return ((long)x << 32) | (uint)z;
    }

    private static void UnhashChunk(long hash, out int x, out int z) {
        x = (int)(hash >> 32);
        z = (int)hash;
    }

    private readonly record struct SerializedChunk(
        Dimension Dimension,
        int Version,
        ChunkColumn Chunk,
        byte[]? Payload,
        Exception? Error);

    private bool ChunkInRange(int x, int z) {
        int dx = x - ChunkX;
        int dz = z - ChunkZ;
        return Math.Max(Math.Abs(dx), Math.Abs(dz)) <= ViewDistance;
    }

    internal void UpdateVisibleEntity(Entity entity) {
        if (!_started || !Player.IsAlive || Player.Dimension is null) {
            return;
        }

        lock (_lock) {
            RefreshVisibleEntity(entity, Player.Dimension.World is Tickable tickable ? tickable.TickValue : 0);
        }
    }

    internal void HideVisibleEntity(Entity entity) {
        lock (_lock) {
            HideEntity(entity);
        }
    }

    private void UpdateVisibleChunks(Dimension dimension) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("PlayerChunkRendering.UpdateVisibleChunks") : default;
        ulong tick = dimension.World is Tickable tickable ? tickable.TickValue : 0;
        _visibilityChunkBuffer.Clear();

        for (int dx = -EntityVisibilityRadiusChunks; dx <= EntityVisibilityRadiusChunks; dx++) {
            for (int dz = -EntityVisibilityRadiusChunks; dz <= EntityVisibilityRadiusChunks; dz++) {
                _visibilityChunkBuffer.Add(HashChunk(ChunkX + dx, ChunkZ + dz));
            }
        }

        _visibilityRemoveBuffer.Clear();
        foreach (long hash in _visibleChunks) {
            if (!_visibilityChunkBuffer.Contains(hash)) {
                _visibilityRemoveBuffer.Add(hash);
            }
        }

        for (int i = 0; i < _visibilityRemoveBuffer.Count; i++) {
            long hash = _visibilityRemoveBuffer[i];
            UnhashChunk(hash, out int x, out int z);
            foreach (Entity entity in dimension.GetEntities(x, z)) {
                HideEntity(entity);
            }

            _visibleChunks.Remove(hash);
        }

        foreach (long hash in _visibilityChunkBuffer) {
            _visibleChunks.Add(hash);

            UnhashChunk(hash, out int x, out int z);
            foreach (Entity entity in dimension.GetEntities(x, z)) {
                RefreshVisibleEntity(entity, tick);
            }
        }
    }

    private void RefreshVisibleEntity(Entity entity, ulong tick) {
        if (ReferenceEquals(entity, Player) || entity.PendingDespawn) {
            HideEntity(entity);
            return;
        }

        if (!entity.IsAlive && !VisibleActorIds.ContainsKey(entity.RuntimeId)) {
            return;
        }

        float dx = entity.Location.X - Player.Location.X;
        float dy = entity.Location.Y - Player.Location.Y;
        float dz = entity.Location.Z - Player.Location.Z;
        if ((dx * dx) + (dy * dy) + (dz * dz) > EntityVisibilityRadiusSquared) {
            HideEntity(entity);
            return;
        }

        if (VisibleActorIds.ContainsKey(entity.RuntimeId)) {
            return;
        }

        entity.SpawnTo(Player, tick);
        VisibleActorIds[entity.RuntimeId] = entity.UniqueId;
    }

    private void HideEntity(Entity entity) {
        if (!VisibleActorIds.Remove(entity.RuntimeId, out long uniqueId)) {
            return;
        }

        Player.Send(new RemoveActorPacket {
            ActorUniqueId = uniqueId
        });
    }

    private void HideAllVisibleEntities() {
        foreach ((_, long uniqueId) in VisibleActorIds) {
            Player.Send(new RemoveActorPacket {
                ActorUniqueId = uniqueId
            });
        }
    }

    private void SendChunkChestVisualUpdates(Dimension dimension, int chunkX, int chunkZ) {
        ChunkColumn? chunk = dimension.GetLoadedChunk(chunkX, chunkZ);
        if (chunk is null) {
            return;
        }

        foreach (BlockLevelStorage storage in chunk.GetAllBlockStorages()) {
            BlockPos position = storage.GetPosition();
            var block = dimension.GetBlock(position.X, position.Y, position.Z);
            block?.OnRender(Player, position.X, position.Y, position.Z);
        }
    }
}
