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
    private readonly Dictionary<ulong, long> _visibleEntityUniqueIds = [];

    private int _currentChunkX = int.MinValue;
    private int _currentChunkZ = int.MinValue;

    private int _publisherChunkX = int.MinValue;
    private int _publisherChunkZ = int.MinValue;

    private int _scanRadius;
    private int _scanX;
    private int _scanZ;

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

            ViewDistance = viewDistance;
            ResetScan();
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
            UpdateTrackedChunkPosition();
            if (Player.Dimension is not null)
            {
                UpdateSimulationChunks(Player.Dimension);
            }
            ResetScan();
            SendPublisherUpdate(includeSavedChunks: true);
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
            _visibleEntityUniqueIds.Clear();
            UpdateTrackedChunkPosition();
            ResetScan();
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
            UpdateSimulationChunks(Player.Dimension);

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

        lock (_lock)
        {
            Dimension dimension = Player.Dimension;
            int chunkX = WorldToChunk(Player.Location.X);
            int chunkZ = WorldToChunk(Player.Location.Z);

            UpdateChunkPosition(chunkX, chunkZ);
            UnloadChunks(dimension, clearClient: true);
            UpdateSimulationChunks(dimension);
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
        List<DataPacket> packets = [];
        List<(long Hash, int X, int Z)> sentChunks = [];

        SendReadyChunks(packets, sentChunks);
        RequestChunks(dimension);
        SendReadyChunks(packets, sentChunks);

        if (packets.Count == 0)
        {
            return;
        }

        Player.Send([.. packets]);

        foreach ((long hash, int x, int z) in sentChunks)
        {
            if (!_loadedChunks.Add(hash))
            {
                continue;
            }

            dimension.AddChunkViewer(x, z);
            SendChunkChestVisualUpdates(dimension, x, z);
        }
    }

    private void RequestChunks(Dimension dimension)
    {
        Span<(int X, int Z)> requests = stackalloc (int X, int Z)[ChunksPerTick];
        int requestCount = 0;

        while (requestCount < ChunksPerTick && NextChunkPosition(out int x, out int z))
        {
            long hash = HashChunk(x, z);
            if (_loadedChunks.Contains(hash) || _requestedChunks.Contains(hash))
            {
                continue;
            }

            _requestedChunks.Add(hash);
            requests[requestCount++] = (x, z);
        }

        if (requestCount == 0)
        {
            return;
        }

        dimension.RequestChunks(requests[..requestCount], chunk =>
        {
            lock (_lock)
            {
                if (!_started || Player.Dimension != dimension || !_requestedChunks.Contains(chunk.Hash))
                {
                    return;
                }

                if (_loadedChunks.Contains(chunk.Hash) || !ChunkInRange(chunk.X, chunk.Z))
                {
                    _requestedChunks.Remove(chunk.Hash);
                    return;
                }

                _readyChunks.Enqueue(chunk);
            }
        });
    }

    private void SendReadyChunks(List<DataPacket> packets, List<(long Hash, int X, int Z)> sentChunks)
    {
        while (packets.Count < ChunksPerTick && _readyChunks.Count > 0)
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

            packets.Add(new LevelChunkPacket
            {
                ChunkX = chunk.X,
                ChunkZ = chunk.Z,
                Dimension = (int)chunk.Type,
                SubChunkCount = (uint)chunk.GetSubChunkSendCount(),
                CacheEnabled = false,
                RawPayload = payload
            });

            sentChunks.Add((chunk.Hash, chunk.X, chunk.Z));
        }
    }

    private void UnloadChunks(Dimension dimension, bool clearClient, bool force = false)
    {
        if (_loadedChunks.Count == 0)
        {
            return;
        }

        List<long> unloadedChunks = [];

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

            unloadedChunks.Add(hash);
        }

        for (int i = 0; i < unloadedChunks.Count; i++)
        {
            _loadedChunks.Remove(unloadedChunks[i]);
        }
    }

    private bool UpdateChunkPosition(int chunkX, int chunkZ)
    {
        if (chunkX == _currentChunkX && chunkZ == _currentChunkZ)
        {
            return false;
        }

        _currentChunkX = chunkX;
        _currentChunkZ = chunkZ;
        ResetScan();
        return true;
    }

    private void ResetScan()
    {
        _scanRadius = 0;
        _scanX = 0;
        _scanZ = 0;
    }

    private bool NextChunkPosition(out int x, out int z)
    {
        while (_scanRadius <= ViewDistance)
        {
            if (_scanRadius == 0)
            {
                _scanRadius = 1;
                _scanX = -1;
                _scanZ = -1;
                x = _currentChunkX;
                z = _currentChunkZ;
                return true;
            }

            while (_scanZ <= _scanRadius)
            {
                while (_scanX <= _scanRadius)
                {
                    int offsetX = _scanX++;
                    int offsetZ = _scanZ;

                    if (Math.Max(Math.Abs(offsetX), Math.Abs(offsetZ)) != _scanRadius)
                    {
                        continue;
                    }

                    x = _currentChunkX + offsetX;
                    z = _currentChunkZ + offsetZ;
                    return true;
                }

                _scanX = -_scanRadius;
                _scanZ++;
            }

            _scanRadius++;
            _scanX = -_scanRadius;
            _scanZ = -_scanRadius;
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
            _visibleEntityUniqueIds.Clear();
            _started = false;
            _currentChunkX = int.MinValue;
            _currentChunkZ = int.MinValue;
            _publisherChunkX = int.MinValue;
            _publisherChunkZ = int.MinValue;
            ResetScan();
        }
    }

    private void UpdateTrackedChunkPosition()
    {
        _currentChunkX = WorldToChunk(Player.Location.X);
        _currentChunkZ = WorldToChunk(Player.Location.Z);

        if (_publisherChunkX == int.MinValue)
        {
            _publisherChunkX = _currentChunkX;
            _publisherChunkZ = _currentChunkZ;
        }
    }

    private void UpdateSimulationChunks(Dimension dimension)
    {
        int simulationDistance = Math.Clamp(dimension.World?.Server?.Properties.SimulationDistance ?? 4, 0, 120);

        for (int dx = -simulationDistance; dx <= simulationDistance; dx++)
        {
            for (int dz = -simulationDistance; dz <= simulationDistance; dz++)
            {
                int x = _currentChunkX + dx;
                int z = _currentChunkZ + dz;
                ChunkColumn? chunk = dimension.GetChunk(x, z);
                if (chunk is not null)
                {
                    chunk.Simulated = true;
                }
            }
        }
    }

    private void SendPublisherUpdate(bool includeSavedChunks)
    {
        Player.Send(CreateChunkPublisherPacket(includeSavedChunks));
        _publisherChunkX = _currentChunkX;
        _publisherChunkZ = _currentChunkZ;
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
        return FloorDiv((int)MathF.Floor(coordinate), 16);
    }

    private static int FloorDiv(int value, int divisor)
    {
        int quotient = value / divisor;
        int remainder = value % divisor;

        if (remainder != 0 && ((remainder < 0) != (divisor < 0)))
        {
            quotient--;
        }

        return quotient;
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
        int dx = x - _currentChunkX;
        int dz = z - _currentChunkZ;
        return Math.Max(Math.Abs(dx), Math.Abs(dz)) <= ViewDistance;
    }

    private void UpdateVisibleEntities(Dimension dimension)
    {
        ulong tick = dimension.World is Tickable tickable ? tickable.TickValue : 0;
        HashSet<ulong> currentVisible = [];

        foreach (Entity entity in dimension.Entities)
        {
            if (ReferenceEquals(entity, Player))
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

            currentVisible.Add(entity.RuntimeId);

            if (_visibleEntityUniqueIds.ContainsKey(entity.RuntimeId))
            {
                continue;
            }

            entity.SpawnTo(Player, tick);
            _visibleEntityUniqueIds[entity.RuntimeId] = entity.UniqueId;
        }

        if (_visibleEntityUniqueIds.Count == 0)
        {
            return;
        }

        List<ulong> hidden = [];
        foreach ((ulong runtimeId, long uniqueId) in _visibleEntityUniqueIds)
        {
            if (currentVisible.Contains(runtimeId))
            {
                continue;
            }

            Player.Send(new RemoveActorPacket
            {
                EntityUniqueId = uniqueId
            });

            hidden.Add(runtimeId);
        }

        for (int i = 0; i < hidden.Count; i++)
        {
            _visibleEntityUniqueIds.Remove(hidden[i]);
        }
    }

    private void HideAllVisibleEntities()
    {
        foreach ((_, long uniqueId) in _visibleEntityUniqueIds)
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
