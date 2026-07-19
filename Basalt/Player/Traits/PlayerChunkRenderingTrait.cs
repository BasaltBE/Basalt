namespace Basalt.Core.Player.Traits;

using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.Core.Blocks;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Traits;
using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Profiling;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;
using Entity = Basalt.Core.Entities.Entity;

public sealed class PlayerChunkRenderingTrait : PlayerTrait
{
    private const int ChunksPerTick = 64;

    public new static string Identifier => "chunk_rendering";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];

    private readonly Lock _lock = new();
    private readonly HashSet<long> _loadedChunks = [];
    private readonly HashSet<long> _requestedChunks = [];
    private readonly Queue<ChunkColumn> _readyChunks = [];
    private readonly List<long> _unloadBuffer = [];
    private readonly List<DataPacket> _sendBuffer = [];
    private readonly List<(long Hash, int X, int Z)> _sentChunkBuffer = [];
    private readonly HashSet<ulong> _visibleThisTick = [];
    private readonly List<ulong> _hideBuffer = [];

    /// <summary>
    /// Maps entity runtime ID to its unique ID for all entities currently visible to this player.
    /// </summary>
    public Dictionary<ulong, long> VisibleActorIds { get; } = [];

    public int ChunkX { get; private set; } = int.MinValue;
    public int ChunkZ { get; private set; } = int.MinValue;

    private int _publisherChunkX = int.MinValue;
    private int _publisherChunkZ = int.MinValue;

    private int _ringRadius;
    private int _ringIndex;

    private bool _started;

    public int ViewDistance { get; private set; } = 16;
    public int LoadedChunkCount => _loadedChunks.Count;

    public PlayerChunkRenderingTrait(Entity entity) : base(entity)
    {
    }

    public void SetViewDistance(int distance)
    {
        ViewDistance = Math.Clamp(distance, 1, 120);
    }

    public void ApplyViewDistance(int distance)
    {
        lock (_lock)
        {
            int viewDistance = Math.Clamp(distance, 1, 120);
            if (ViewDistance == viewDistance)
            {
                return;
            }

            // Logger.Info($"ApplyViewDistance for {Player.Username}: {ViewDistance} → {viewDistance}");
            ViewDistance = viewDistance;
            ResetRingScan();
            _requestedChunks.Clear();
            _readyChunks.Clear();

            if (!_started || Player.Dimension is null)
            {
                return;
            }

            UpdateTrackedChunkPosition();
            UnloadChunks(Player.Dimension, clearClient: true);
            SendPublisherUpdate(includeSavedChunks: true);
        }
    }

    public void StartChunkLoad()
    {
        lock (_lock)
        {
            _started = true;
            _loadedChunks.Clear();
            _requestedChunks.Clear();
            _readyChunks.Clear();
            UpdateTrackedChunkPosition();
            ResetRingScan();
            SendPublisherUpdate(includeSavedChunks: true);

            if (Player.Dimension is not null)
            {
                RequestAndSendAvailableChunks(Player.Dimension);
            }
        }
    }

    public override void OnSpawn(EntitySpawnOptions details)
    {
        UpdateTrackedChunkPosition();
    }

    public override void OnTeleport(EntityTeleportOptions details)
    {
        lock (_lock)
        {
            HideAllVisibleEntities();

            if (Player.Dimension is not null)
            {
                UnloadChunks(Player.Dimension, clearClient: true, force: true);
            }

            _loadedChunks.Clear();
            _requestedChunks.Clear();
            _readyChunks.Clear();
            VisibleActorIds.Clear();
            UpdateTrackedChunkPosition();
            ResetRingScan();
        }
    }

    public override void OnMove(EntityMoveOptions details)
    {
        if (!_started || !Player.IsAlive || Player.Dimension is null)
        {
            return;
        }

        lock (_lock)
        {
            int chunkX = WorldToChunk(details.To.X);
            int chunkZ = WorldToChunk(details.To.Z);

            if (!UpdateChunkPosition(chunkX, chunkZ))
            {
                return;
            }

            UnloadChunks(Player.Dimension, clearClient: true);

            if (Math.Abs(chunkX - _publisherChunkX) > 2 || Math.Abs(chunkZ - _publisherChunkZ) > 2)
            {
                SendPublisherUpdate(includeSavedChunks: true);
            }
        }
    }

    public override void OnTick(TraitOnTickDetails details)
    {
        if (!_started || !Player.IsAlive || Player.Dimension is null)
        {
            return;
        }

        using var __zone = Profiler.BeginZone("PlayerChunkRendering.OnTick");
        lock (_lock)
        {
            Dimension dimension = Player.Dimension;
            int chunkX = WorldToChunk(Player.Location.X);
            int chunkZ = WorldToChunk(Player.Location.Z);

            UpdateChunkPosition(chunkX, chunkZ);
            UnloadChunks(dimension, clearClient: true);
            SendChunks(dimension);
            UpdateVisibleEntities(dimension);
        }
    }

    public override void OnDespawn(EntityDespawnOptions details)
    {
        Clear();
    }

    public override void OnRemove()
    {
        Clear();
    }

    public override EntityTrait Clone(Entity entity)
    {
        PlayerChunkRenderingTrait trait = new(entity);
        trait.SetViewDistance(ViewDistance);
        return trait;
    }

    private void SendChunks(Dimension dimension)
    {
        using var __zone = Profiler.BeginZone("PlayerChunkRendering.SendChunks");
        _sendBuffer.Clear();
        _sentChunkBuffer.Clear();

        SendReadyChunks();

        while (_sendBuffer.Count < ChunksPerTick && NextRingPosition(out int x, out int z))
        {
            long hash = HashChunk(x, z);
            if (_loadedChunks.Contains(hash) || _requestedChunks.Contains(hash))
            {
                continue;
            }

            ChunkColumn chunk = dimension.GetOrCreateChunk(x, z);

            byte[] payload;
            try
            {
                payload = ChunkColumn.Serialize(chunk);
            }
            catch (Exception exception)
            {
                Logger.Err($"Failed to serialize chunk {x}, {z}: {exception.Message}");
                continue;
            }

            _sendBuffer.Add(new LevelChunkPacket
            {
                ChunkX = chunk.X,
                ChunkZ = chunk.Z,
                Dimension = (int)chunk.Type,
                SubChunkCount = (uint)chunk.GetSubChunkSendCount(),
                CacheEnabled = false,
                RawPayload = payload
            });
            _sentChunkBuffer.Add((hash, x, z));
        }

        if (_sendBuffer.Count == 0)
        {
            return;
        }

        Player.Send([.. _sendBuffer]);

        foreach ((long hash, int x, int z) in _sentChunkBuffer)
        {
            if (!_loadedChunks.Add(hash))
            {
                continue;
            }

            dimension.AddChunkViewer(x, z);
            SendChunkChestVisualUpdates(dimension, x, z);
        }
    }

    private void SendReadyChunks()
    {
        while (_sendBuffer.Count < ChunksPerTick && _readyChunks.Count > 0)
        {
            ChunkColumn chunk = _readyChunks.Dequeue();
            _requestedChunks.Remove(chunk.Hash);

            if (_loadedChunks.Contains(chunk.Hash) || !ChunkInRange(chunk.X, chunk.Z))
            {
                continue;
            }

            byte[] payload;

            try
            {
                payload = ChunkColumn.Serialize(chunk);
            }
            catch (Exception exception)
            {
                Logger.Err($"Failed to serialize chunk {chunk.X}, {chunk.Z}: {exception.Message}");
                continue;
            }

            _sendBuffer.Add(new LevelChunkPacket
            {
                ChunkX = chunk.X,
                ChunkZ = chunk.Z,
                Dimension = (int)chunk.Type,
                SubChunkCount = (uint)chunk.GetSubChunkSendCount(),
                CacheEnabled = false,
                RawPayload = payload
            });

            _sentChunkBuffer.Add((chunk.Hash, chunk.X, chunk.Z));
        }
    }

    private void RequestAndSendAvailableChunks(Dimension dimension)
    {
        _sendBuffer.Clear();
        _sentChunkBuffer.Clear();

        while (_sendBuffer.Count < ChunksPerTick && NextRingPosition(out int x, out int z))
        {
            long hash = HashChunk(x, z);
            if (_loadedChunks.Contains(hash) || _requestedChunks.Contains(hash))
            {
                continue;
            }

            ChunkColumn chunk = dimension.GetOrCreateChunk(x, z);

            byte[] payload;
            try
            {
                payload = ChunkColumn.Serialize(chunk);
            }
            catch (Exception exception)
            {
                Logger.Err($"Failed to serialize chunk {x}, {z}: {exception.Message}");
                continue;
            }

            _sendBuffer.Add(new LevelChunkPacket
            {
                ChunkX = chunk.X,
                ChunkZ = chunk.Z,
                Dimension = (int)chunk.Type,
                SubChunkCount = (uint)chunk.GetSubChunkSendCount(),
                CacheEnabled = false,
                RawPayload = payload
            });
            _sentChunkBuffer.Add((hash, x, z));
        }

        if (_sendBuffer.Count == 0)
        {
            return;
        }

        Player.Send([.. _sendBuffer]);

        foreach ((long hash, int x, int z) in _sentChunkBuffer)
        {
            if (!_loadedChunks.Add(hash))
            {
                continue;
            }

            dimension.AddChunkViewer(x, z);
            SendChunkChestVisualUpdates(dimension, x, z);
        }
    }

    private void UnloadChunks(Dimension dimension, bool clearClient, bool force = false)
    {
        if (_loadedChunks.Count == 0)
        {
            return;
        }

        _unloadBuffer.Clear();

        foreach (long hash in _loadedChunks)
        {
            UnhashChunk(hash, out int x, out int z);

            if (!force && ChunkInRange(x, z))
            {
                continue;
            }

            if (clearClient)
            {
                Player.Send(new LevelChunkPacket
                {
                    ChunkX = x,
                    ChunkZ = z,
                    Dimension = (int)dimension.Type,
                    SubChunkCount = 0,
                    CacheEnabled = false,
                    RawPayload = []
                });
            }

            dimension.RemoveChunkViewer(x, z);

            if (!dimension.HasChunkViewers(x, z))
            {
                dimension.UnloadChunk(x, z);
            }

            _unloadBuffer.Add(hash);
        }

        for (int i = 0; i < _unloadBuffer.Count; i++)
        {
            _loadedChunks.Remove(_unloadBuffer[i]);
        }
    }

    private bool UpdateChunkPosition(int chunkX, int chunkZ)
    {
        if (chunkX == ChunkX && chunkZ == ChunkZ)
        {
            return false;
        }

        ChunkX = chunkX;
        ChunkZ = chunkZ;
        ResetRingScan();
        return true;
    }

    private void ResetRingScan()
    {
        _ringRadius = 0;
        _ringIndex = 0;
    }


    private bool NextRingPosition(out int x, out int z)
    {
        while (_ringRadius <= ViewDistance)
        {
            if (_ringRadius == 0)
            {
                _ringRadius = 1;
                _ringIndex = 0;
                x = ChunkX;
                z = ChunkZ;
                return true;
            }

            int r = _ringRadius;
            int perimeterLength = 8 * r;

            if (_ringIndex >= perimeterLength)
            {
                _ringRadius++;
                _ringIndex = 0;
                continue;
            }

            int sideLength = 2 * r;
            int index = _ringIndex++;
            int offsetX;
            int offsetZ;

            if (index < sideLength)
            {
                offsetX = -r + index;
                offsetZ = -r;
            }
            else if (index < sideLength * 2)
            {
                int i = index - sideLength;
                offsetX = r;
                offsetZ = -r + i;
            }
            else if (index < sideLength * 3)
            {
                int i = index - (sideLength * 2);
                offsetX = r - i;
                offsetZ = r;
            }
            else
            {
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

    private void Clear()
    {
        lock (_lock)
        {
            HideAllVisibleEntities();

            if (Player.Dimension is not null)
            {
                UnloadChunks(Player.Dimension, clearClient: false, force: true);
            }

            _loadedChunks.Clear();
            _requestedChunks.Clear();
            _readyChunks.Clear();
            VisibleActorIds.Clear();
            _started = false;
            ChunkX = int.MinValue;
            ChunkZ = int.MinValue;
            _publisherChunkX = int.MinValue;
            _publisherChunkZ = int.MinValue;
            ResetRingScan();
        }
    }

    private void UpdateTrackedChunkPosition()
    {
        ChunkX = WorldToChunk(Player.Location.X);
        ChunkZ = WorldToChunk(Player.Location.Z);

        if (_publisherChunkX == int.MinValue)
        {
            _publisherChunkX = ChunkX;
            _publisherChunkZ = ChunkZ;
        }
    }

    private void SendPublisherUpdate(bool includeSavedChunks)
    {
        Player.Send(CreateChunkPublisherPacket(includeSavedChunks));
        _publisherChunkX = ChunkX;
        _publisherChunkZ = ChunkZ;
    }

    private NetworkChunkPublisherUpdatePacket CreateChunkPublisherPacket(bool includeSavedChunks)
    {
        NetworkChunkPublisherUpdatePacket packet = new()
        {
            CoordinateX = (int)MathF.Floor(Player.Location.X),
            CoordinateY = (int)MathF.Floor(Player.Location.Y),
            CoordinateZ = (int)MathF.Floor(Player.Location.Z),
            Radius = (uint)(ViewDistance << 4),
            SavedChunks = []
        };

        if (!includeSavedChunks)
        {
            return packet;
        }

        foreach (long hash in _loadedChunks)
        {
            UnhashChunk(hash, out int x, out int z);

            if (ChunkInRange(x, z))
            {
                packet.SavedChunks.Add((x, z));
            }
        }

        return packet;
    }

    private static int WorldToChunk(float coordinate)
    {
        return (int)MathF.Floor(coordinate) >> 4;
    }

    private static long HashChunk(int x, int z)
    {
        return ((long)x << 32) | (uint)z;
    }

    private static void UnhashChunk(long hash, out int x, out int z)
    {
        x = (int)(hash >> 32);
        z = (int)hash;
    }

    private bool ChunkInRange(int x, int z)
    {
        int dx = x - ChunkX;
        int dz = z - ChunkZ;
        return Math.Max(Math.Abs(dx), Math.Abs(dz)) <= ViewDistance;
    }

    private void UpdateVisibleEntities(Dimension dimension)
    {
        using var __zone = Profiler.BeginZone("PlayerChunkRendering.UpdateVisibleEntities");
        ulong tick = dimension.World is Tickable tickable ? tickable.TickValue : 0;
        _visibleThisTick.Clear();

        foreach (Entity entity in dimension.Entities)
        {
            if (ReferenceEquals(entity, Player) || entity is Player)
            {
                continue;
            }

            if (!entity.IsAlive || entity.PendingDespawn || entity.Dimension != dimension)
            {
                continue;
            }

            int chunkX = WorldToChunk(entity.Location.X);
            int chunkZ = WorldToChunk(entity.Location.Z);
            long hash = HashChunk(chunkX, chunkZ);

            if (!_loadedChunks.Contains(hash))
            {
                continue;
            }

            _visibleThisTick.Add(entity.RuntimeId);

            if (VisibleActorIds.ContainsKey(entity.RuntimeId))
            {
                continue;
            }

            entity.SpawnTo(Player, tick);
            VisibleActorIds[entity.RuntimeId] = entity.UniqueId;
        }

        if (VisibleActorIds.Count == 0)
        {
            return;
        }

        _hideBuffer.Clear();

        foreach ((ulong runtimeId, long uniqueId) in VisibleActorIds)
        {
            if (_visibleThisTick.Contains(runtimeId))
            {
                continue;
            }

            Player.Send(new RemoveActorPacket
            {
                EntityUniqueId = uniqueId
            });

            _hideBuffer.Add(runtimeId);
        }

        for (int i = 0; i < _hideBuffer.Count; i++)
        {
            VisibleActorIds.Remove(_hideBuffer[i]);
        }
    }

    private void HideAllVisibleEntities()
    {
        foreach ((_, long uniqueId) in VisibleActorIds)
        {
            Player.Send(new RemoveActorPacket
            {
                EntityUniqueId = uniqueId
            });
        }
    }

    private void SendChunkChestVisualUpdates(Dimension dimension, int chunkX, int chunkZ)
    {
        ChunkColumn? chunk = dimension.GetChunk(chunkX, chunkZ);
        if (chunk is null)
        {
            return;
        }

        foreach (BlockLevelStorage storage in chunk.GetAllBlockStorages())
        {
            BlockPos position = storage.GetPosition();
            var block = dimension.GetBlock(position.X, position.Y, position.Z);
            block?.OnRender(Player, position.X, position.Y, position.Z);
        }
    }
}
