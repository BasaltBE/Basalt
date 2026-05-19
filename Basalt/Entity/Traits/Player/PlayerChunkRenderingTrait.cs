using Basalt.Entity.Traits.Types;
using Basalt.Block;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.Traits;
using ChunkColumn = Basalt.World.Dimension.Chunk.Chunk;

namespace Basalt.Entity.Traits.PlayerTraits;

public sealed class PlayerChunkRenderingTrait : PlayerTrait
{
    private const int MaxChunkPayloadBytesPerBatch = 350_000;
    private const int ChunkBatchSize = 12; // TODO: Move this into server options

    public new static string Identifier => "chunk_rendering";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];

    private readonly Lock _lock = new();

    private readonly HashSet<long> _loadedChunks = [];
    private readonly HashSet<long> _pendingChunks = [];
    private readonly Dictionary<int, int[]> _offsetCache = [];

    private readonly List<ChunkColumn> _sendQueue = [];

    private byte[]? _emptyChunkPayload;

    private int _sendGeneration;
    private int _tickCounter;
    private int _chunkQueueIndex;

    private bool _isTicking;

    private int _lastPublisherChunkX = int.MinValue;
    private int _lastPublisherChunkZ = int.MinValue;

    private int _currentChunkX = int.MinValue;
    private int _currentChunkZ = int.MinValue;

    public int ViewDistance { get; private set; } = 6;

    public int LoadedChunkCount => _loadedChunks.Count;

    public PlayerChunkRenderingTrait(Entity entity) : base(entity)
    {
    }

    public void SetViewDistance(int distance)
    {
        ViewDistance = Math.Clamp(distance, 1, 96);
    }

    public void ApplyViewDistance(int distance)
    {
        lock (_lock)
        {
            if (ViewDistance == distance)
            {
                return;
            }

            SetViewDistance(distance);
            _chunkQueueIndex = 0;

            if (Player.Dimension is null)
            {
                return;
            }

            Player.Send(CreateChunkPublisherPacket(includeSavedChunks: true));
            QueueChunks();
            SendQueuedChunks();
        }
    }

    public void StartChunkLoad()
    {
        lock (_lock)
        {
            Player.Send(CreateChunkPublisherPacket(includeSavedChunks: true));
            _chunkQueueIndex = 0;
            QueueChunks();
            SendQueuedChunks();
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
            ReleaseLoadedChunks(unloadChunks: true);

            _loadedChunks.Clear();
            _pendingChunks.Clear();
            _sendQueue.Clear();

            _sendGeneration++;
            _tickCounter = 0;

            _emptyChunkPayload = null;

            UpdateTrackedChunkPosition();
            _chunkQueueIndex = 0;
        }
    }

    public override void OnMove(EntityMoveOptions details)
    {
        if (!Player.IsAlive || Player.Dimension is null)
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

            UnloadOutOfRangeChunks();
            QueueChunks();
            SendQueuedChunks();

            Player.Send(CreateChunkPublisherPacket(includeSavedChunks: true));
        }
    }

    public override void OnTick(TraitOnTickDetails details)
    {
        if (_isTicking || !Player.IsAlive || Player.Dimension is null)
        {
            return;
        }

        lock (_lock)
        {
            _isTicking = true;

            try
            {
                int chunkX = WorldToChunk(Player.Position.X);
                int chunkZ = WorldToChunk(Player.Position.Z);

                if (_currentChunkX == int.MinValue)
                {
                    _currentChunkX = chunkX;
                    _currentChunkZ = chunkZ;

                    _lastPublisherChunkX = chunkX;
                    _lastPublisherChunkZ = chunkZ;
                }

                bool moved = UpdateChunkPosition(chunkX, chunkZ);

                if ((_tickCounter & 3) == 0)
                {
                    UnloadOutOfRangeChunks();
                }

                if (moved)
                {
                    QueueChunks();
                }
                else if (HasChunksLeftToQueue())
                {
                    QueueChunks();
                }

                if (_sendQueue.Count > 0)
                {
                    SendQueuedChunks();
                }

                bool updatePublisher =
                    Math.Abs(chunkX - _lastPublisherChunkX) > 2 ||
                    Math.Abs(chunkZ - _lastPublisherChunkZ) > 2;

                if (updatePublisher)
                {
                    Player.Send(CreateChunkPublisherPacket(includeSavedChunks: true));

                    _lastPublisherChunkX = chunkX;
                    _lastPublisherChunkZ = chunkZ;
                }

                _tickCounter++;
            }
            finally
            {
                _isTicking = false;
            }
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

    private void SendQueuedChunks()
    {
        if (_sendQueue.Count == 0 || Player.Dimension is null)
        {
            return;
        }

        if (!Player.IsAlive)
        {
            CancelQueuedChunks(0);
            return;
        }

        int generation = _sendGeneration;
        int end = Math.Min(ChunkBatchSize, _sendQueue.Count);

        List<DataPacket> packets = [];
        List<long> sentChunks = [];

        int payloadSize = 0;
        int processed = 0;

        for (int i = 0; i < end; i++)
        {
            if (generation != _sendGeneration || Player.Dimension is null)
            {
                CancelQueuedChunks(i);
                return;
            }

            ChunkColumn chunk = _sendQueue[i];
            byte[] payload;

            try
            {
                payload = ChunkColumn.Serialize(chunk);
            }
            catch (Exception exception)
            {
                Logger.Err($"Failed to serialize chunk {chunk.X}, {chunk.Z}: {exception.Message}");

                _pendingChunks.Remove(chunk.Hash);
                processed++;

                continue;
            }

            int packetSize = payload.Length + 64;

            if (packets.Count > 0 &&
                payloadSize + packetSize > MaxChunkPayloadBytesPerBatch)
            {
                break;
            }

            payloadSize += packetSize;
            processed++;

            packets.Add(new LevelChunkPacket
            {
                ChunkX = chunk.X,
                ChunkZ = chunk.Z,
                Dimension = (int)chunk.Type,
                SubChunkCount = (uint)chunk.GetSubChunkSendCount(),
                CacheEnabled = false,
                RawPayload = payload
            });

            sentChunks.Add(chunk.Hash);
        }

        try
        {
            if (packets.Count > 0)
            {
                Player.Send([.. packets]);
            }
        }
        catch (Exception exception)
        {
            Logger.Warn($"Chunk send failed for {Player.Username}: {exception.Message}");

            foreach (long hash in sentChunks)
            {
                _pendingChunks.Remove(hash);
            }

            return;
        }

        for (int i = 0; i < processed; i++)
        {
            _sendQueue.RemoveAt(0);
        }

        foreach (long hash in sentChunks)
        {
            _pendingChunks.Remove(hash);

            if (!_loadedChunks.Add(hash))
            {
                continue;
            }

            UnhashChunk(hash, out int x, out int z);

            Player.Dimension.AddChunkViewer(x, z);
            SendChunkChestVisualUpdates(x, z);
        }
    }

    private void QueueChunks()
    {
        if (Player.Dimension is null)
        {
            return;
        }

        int[] offsets = GetChunkOffsets(ViewDistance);

        int chunkX = WorldToChunk(Player.Position.X);
        int chunkZ = WorldToChunk(Player.Position.Z);

        while (_chunkQueueIndex < offsets.Length)
        {
            int x = chunkX + offsets[_chunkQueueIndex++];
            int z = chunkZ + offsets[_chunkQueueIndex++];

            long hash = HashChunk(x, z);

            if (_loadedChunks.Contains(hash) || _pendingChunks.Contains(hash))
            {
                continue;
            }

            _pendingChunks.Add(hash);
            _sendQueue.Add(Player.Dimension.GetOrCreateChunk(x, z));
        }
    }

    private void UnloadOutOfRangeChunks()
    {
        if (_loadedChunks.Count == 0 || Player.Dimension is null)
        {
            return;
        }

        int centerX = WorldToChunk(Player.Position.X);
        int centerZ = WorldToChunk(Player.Position.Z);

        List<(int X, int Z)> unloadList = [];

        foreach (long hash in _loadedChunks)
        {
            UnhashChunk(hash, out int x, out int z);

            int dx = x - centerX;
            int dz = z - centerZ;

            if (Math.Max(Math.Abs(dx), Math.Abs(dz)) > ViewDistance)
            {
                unloadList.Add((x, z));
            }
        }

        if (unloadList.Count == 0)
        {
            return;
        }

        _emptyChunkPayload ??= ChunkColumn.Serialize(
            new ChunkColumn(0, 0, Player.Dimension.Type));

        int unloadCount = Math.Min(
            Math.Max(24, ChunkBatchSize * 3),
            unloadList.Count);

        for (int i = 0; i < unloadCount; i++)
        {
            (int x, int z) = unloadList[i];

            Player.Send(new LevelChunkPacket
            {
                ChunkX = x,
                ChunkZ = z,
                Dimension = (int)Player.Dimension.Type,
                SubChunkCount = 0,
                CacheEnabled = false,
                RawPayload = _emptyChunkPayload
            });

            long hash = HashChunk(x, z);

            _loadedChunks.Remove(hash);
            _pendingChunks.Remove(hash);

            Player.Dimension.RemoveChunkViewer(x, z);

            if (!Player.Dimension.HasChunkViewers(x, z))
            {
                Player.Dimension.UnloadChunk(x, z);
            }
        }
    }

    private bool UpdateChunkPosition(int chunkX, int chunkZ)
    {
        bool moved =
            chunkX != _currentChunkX ||
            chunkZ != _currentChunkZ;

        if (!moved)
        {
            return false;
        }

        int dx = Math.Abs(chunkX - _currentChunkX);
        int dz = Math.Abs(chunkZ - _currentChunkZ);

        if (dx > 2 || dz > 2)
        {
            _pendingChunks.Clear();
            _sendQueue.Clear();
        }

        _currentChunkX = chunkX;
        _currentChunkZ = chunkZ;
        _chunkQueueIndex = 0;

        return true;
    }

    private void ReleaseLoadedChunks(bool unloadChunks)
    {
        if (Player.Dimension is null)
        {
            return;
        }

        foreach (long hash in _loadedChunks)
        {
            UnhashChunk(hash, out int x, out int z);

            Player.Dimension.RemoveChunkViewer(x, z);

            if (unloadChunks && !Player.Dimension.HasChunkViewers(x, z))
            {
                Player.Dimension.UnloadChunk(x, z);
            }
        }
    }

    private void CancelQueuedChunks(int startIndex)
    {
        for (int i = startIndex; i < _sendQueue.Count; i++)
        {
            _pendingChunks.Remove(_sendQueue[i].Hash);
        }

        _sendQueue.Clear();
    }

    private void Clear()
    {
        lock (_lock)
        {
            ReleaseLoadedChunks(unloadChunks: true);

            _loadedChunks.Clear();
            _pendingChunks.Clear();
            _sendQueue.Clear();

            _currentChunkX = int.MinValue;
            _currentChunkZ = int.MinValue;
            _chunkQueueIndex = 0;
        }
    }

    private void UpdateTrackedChunkPosition()
    {
        int chunkX = WorldToChunk(Player.Position.X);
        int chunkZ = WorldToChunk(Player.Position.Z);

        _currentChunkX = chunkX;
        _currentChunkZ = chunkZ;

        _lastPublisherChunkX = chunkX;
        _lastPublisherChunkZ = chunkZ;
    }

    private bool HasChunksLeftToQueue()
    {
        return _chunkQueueIndex < GetChunkOffsets(ViewDistance).Length;
    }

    private NetworkChunkPublisherUpdatePacket CreateChunkPublisherPacket(bool includeSavedChunks)
    {
        NetworkChunkPublisherUpdatePacket packet = new()
        {
            CoordinateX = (int)MathF.Floor(Player.Position.X),
            CoordinateY = (int)MathF.Floor(Player.Position.Y),
            CoordinateZ = (int)MathF.Floor(Player.Position.Z),
            Radius = (uint)(ViewDistance << 4),
            SavedChunks = []
        };

        if (!includeSavedChunks)
        {
            return packet;
        }

        int centerX = WorldToChunk(Player.Position.X);
        int centerZ = WorldToChunk(Player.Position.Z);

        foreach (long hash in _loadedChunks)
        {
            UnhashChunk(hash, out int x, out int z);

            int dx = x - centerX;
            int dz = z - centerZ;

            if (Math.Max(Math.Abs(dx), Math.Abs(dz)) <= ViewDistance)
            {
                packet.SavedChunks.Add((x, z));
            }
        }

        return packet;
    }

    private int[] GetChunkOffsets(int distance)
    {
        if (_offsetCache.TryGetValue(distance, out int[]? cached))
        {
            return cached;
        }

        List<(int Distance, int X, int Z)> offsets = [];

        for (int dx = -distance; dx <= distance; dx++)
        {
            for (int dz = -distance; dz <= distance; dz++)
            {
                int dist = dx * dx + dz * dz;
                offsets.Add((dist, dx, dz));
            }
        }

        offsets.Sort(static (a, b) => a.Distance.CompareTo(b.Distance));

        int[] packed = new int[offsets.Count * 2];

        int index = 0;

        foreach ((_, int x, int z) in offsets)
        {
            packed[index++] = x;
            packed[index++] = z;
        }

        _offsetCache[distance] = packed;

        return packed;
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

    private void SendChunkChestVisualUpdates(int chunkX, int chunkZ)
    {
        if (Player.Dimension is null)
        {
            return;
        }

        ChunkColumn? chunk = Player.Dimension.GetChunk(chunkX, chunkZ);
        if (chunk is null)
        {
            return;
        }

        foreach (BlockLevelStorage storage in chunk.GetAllBlockStorages())
        {
            BlockPos position = storage.GetPosition();
            var block = Player.Dimension.GetBlock(position.X, position.Y, position.Z);
            block?.OnRender(Player, position.X, position.Y, position.Z);
        }
    }

}
