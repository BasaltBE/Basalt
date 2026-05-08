using Basalt.Entity.Traits.Types;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Traits;
using ChunkColumn = Basalt.World.Dimension.Chunk.Chunk;

namespace Basalt.Entity.Traits.PlayerTraits;

public sealed class PlayerChunkRenderingTrait : PlayerTrait
{
    private const int MaxChunkPayloadBytesPerBatch = 350_000;
    public new static string Identifier => "chunk_rendering";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];

    private readonly Lock _sync = new();
    private readonly HashSet<long> _chunks = [];
    private readonly HashSet<long> _pending = [];
    private readonly Dictionary<int, int[]> _offsetCache = [];
    private List<ChunkColumn> _chunkQueue = [];
    private byte[]? _emptyChunkData;
    private int _sendGeneration;
    private bool _tickInProgress;
    private int _updateTickCounter;
    private int _lastChunkX = int.MinValue;
    private int _lastChunkZ = int.MinValue;
    private int _queueChunkX = int.MinValue;
    private int _queueChunkZ = int.MinValue;

    public int ViewDistance { get; private set; } = 6;
    public int LoadedChunkCount => _chunks.Count;

    public PlayerChunkRenderingTrait(Entity entity) : base(entity)
    {
    }

    public void SetViewDistance(int viewDistance)
    {
        ViewDistance = Math.Clamp(viewDistance, 1, 96);
    }

    public void ApplyViewDistance(int viewDistance)
    {
        lock (_sync)
        {
            if (ViewDistance == viewDistance)
            {
                return;
            }

            SetViewDistance(viewDistance);
            
            if (Player.Dimension is null)
            {
                return;
            }

            Player.Send(CreateChunkPublisherUpdatePacket(true));
            
            int batchSize = GetChunkBatchSize();
            QueueNextChunks(batchSize * 8);
            SendQueuedChunks();
        }
    }

    public override void OnTick(TraitOnTickDetails details)
    {
        if (_tickInProgress || !Player.IsAlive || Player.Dimension is null)
        {
            return;
        }

        lock (_sync)
        {
            _tickInProgress = true;
            try
            {
                int currentChunkX = FloorDiv((int)MathF.Floor(Player.Position.X), 16);
                int currentChunkZ = FloorDiv((int)MathF.Floor(Player.Position.Z), 16);
                
                // Initialize position tracking on first tick
                if (_queueChunkX == int.MinValue || _queueChunkZ == int.MinValue)
                {
                    _queueChunkX = currentChunkX;
                    _queueChunkZ = currentChunkZ;
                    _lastChunkX = currentChunkX;
                    _lastChunkZ = currentChunkZ;
                }
                
                bool chunkChanged = HandleChunkMovement(currentChunkX, currentChunkZ);

                // Send any queued chunks first
                if (_chunkQueue.Count > 0)
                {
                    SendQueuedChunks();
                }

                // Remove out of range chunks periodically
                if ((_updateTickCounter & 3) == 0)
                {
                    RemoveOutOfRangeChunks();
                }
                
                // Keep queueing chunks every tick until all are loaded
                // Scale with batch size for consistent loading speed
                int batchSize = GetChunkBatchSize();
                int queueLimit = chunkChanged ? batchSize * 12 : batchSize * 6;
                
                QueueNextChunks(queueLimit);
                
                // Send newly queued chunks
                SendQueuedChunks();

                _updateTickCounter++;
                
                // Update chunk publisher when position changes significantly
                bool shouldUpdate = Math.Abs(currentChunkX - _lastChunkX) > 2 ||
                                    Math.Abs(currentChunkZ - _lastChunkZ) > 2;

                if (shouldUpdate)
                {
                    Player.Send(CreateChunkPublisherUpdatePacket(true));
                    _lastChunkX = currentChunkX;
                    _lastChunkZ = currentChunkZ;
                }
            }
            finally
            {
                _tickInProgress = false;
            }
        }
    }

    public override void OnSpawn(EntitySpawnOptions details)
    {
        // Initialize position tracking to current player position
        int currentChunkX = FloorDiv((int)MathF.Floor(Player.Position.X), 16);
        int currentChunkZ = FloorDiv((int)MathF.Floor(Player.Position.Z), 16);
        _queueChunkX = currentChunkX;
        _queueChunkZ = currentChunkZ;
        _lastChunkX = currentChunkX;
        _lastChunkZ = currentChunkZ;
    }

    public void StartChunkLoad()
    {
        lock (_sync)
        {
            Player.Send(CreateChunkPublisherUpdatePacket(true));
            int batchSize = GetChunkBatchSize();
            QueueNextChunks(batchSize * 8);
            SendQueuedChunks();
        }
    }

    public override void OnTeleport(EntityTeleportOptions details)
    {
        lock (_sync)
        {
            ReleaseTrackedChunks(tryUnload: true);
            ReleaseQueuedChunks(0);
            _sendGeneration++;
            _pending.Clear();
            _chunkQueue.Clear();
            _emptyChunkData = null;
            _updateTickCounter = 0;
            
            // Update position tracking to new teleport location
            int currentChunkX = FloorDiv((int)MathF.Floor(Player.Position.X), 16);
            int currentChunkZ = FloorDiv((int)MathF.Floor(Player.Position.Z), 16);
            _queueChunkX = currentChunkX;
            _queueChunkZ = currentChunkZ;
            _lastChunkX = currentChunkX;
            _lastChunkZ = currentChunkZ;
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

    public override void OnMove(EntityMoveOptions details)
    {
        if (Player.Dimension is null || !Player.IsAlive)
        {
            return;
        }

        lock (_sync)
        {
            int currentChunkX = FloorDiv((int)MathF.Floor(details.To.X), 16);
            int currentChunkZ = FloorDiv((int)MathF.Floor(details.To.Z), 16);
            if (!HandleChunkMovement(currentChunkX, currentChunkZ))
            {
                return;
            }

            int batchSize = GetChunkBatchSize();
            QueueNextChunks(batchSize * 12);
            SendQueuedChunks();
            Player.Send(CreateChunkPublisherUpdatePacket(true));
        }
    }

    private void SendQueuedChunks()
    {
        if (_chunkQueue.Count == 0 || Player.Dimension is null)
        {
            return;
        }

        int generation = _sendGeneration;
        int batchSize = GetChunkBatchSize();
        int count = _chunkQueue.Count;
        bool sendFailed = false;
        int totalSent = 0;
        int start = 0;

        for (; start < count;)
        {
            if (generation != _sendGeneration || !Player.IsAlive || Player.Dimension is null)
            {
                Logger.Warn($"[{Player.Username}] Chunk send interrupted - Generation changed or player died");
                // Generation changed or player died - remove unsent chunks from pending
                for (int i = start; i < _chunkQueue.Count; i++)
                {
                    _pending.Remove(_chunkQueue[i].Hash);
                }
                ReleaseQueuedChunks(start);
                _chunkQueue.Clear();
                return;
            }

            int end = Math.Min(start + batchSize, count);
            List<DataPacket> packets = new(end - start);
            List<long> sentInBatch = new(end - start);
            List<long> failedInBatch = new(end - start);
            int payloadBytes = 0;

            for (int i = start; i < end; i++)
            {
                ChunkColumn chunk = _chunkQueue[i];
                long hash = chunk.Hash;
                
                byte[] payload;
                try
                {
                    payload = ChunkColumn.Serialize(chunk);
                }
                catch (Exception ex)
                {
                    Logger.Err($"Failed to serialize chunk at {chunk.X}, {chunk.Z}: {ex.Message}");
                    _pending.Remove(hash);
                    failedInBatch.Add(hash);
                    continue;
                }
                
                int packetBytes = payload.Length + 64;
                
                // If this chunk would exceed the payload limit, stop this batch
                if (packets.Count > 0 && payloadBytes + packetBytes > MaxChunkPayloadBytesPerBatch)
                {
                    break;
                }

                sentInBatch.Add(hash);
                payloadBytes += packetBytes;
                packets.Add(new LevelChunkPacket
                {
                    ChunkX = chunk.X,
                    ChunkZ = chunk.Z,
                    Dimension = (int)chunk.Type,
                    SubChunkCount = (uint)chunk.GetSubChunkSendCount(),
                    CacheEnabled = false,
                    RawPayload = payload
                });
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
                
                // Remove failed chunks from pending so they can be retried
                for (int i = 0; i < sentInBatch.Count; i++)
                {
                    _pending.Remove(sentInBatch[i]);
                }

                sendFailed = true;
                break;
            }

            // Mark chunks as successfully sent
            for (int i = 0; i < sentInBatch.Count; i++)
            {
                long hash = sentInBatch[i];
                bool added = _chunks.Add(hash);
                _pending.Remove(hash);
                if (added)
                {
                    UnhashChunk(hash, out int x, out int z);
                    Player.Dimension?.AddChunkViewer(x, z);
                }
            }

            for (int i = 0; i < failedInBatch.Count; i++)
            {
                long hash = failedInBatch[i];
                _chunks.Remove(hash);
                if (Player.Dimension is not null)
                {
                    UnhashChunk(hash, out int x, out int z);
                    if (!Player.Dimension.HasChunkViewers(x, z))
                    {
                        Player.Dimension.UnloadChunk(x, z);
                    }
                }
            }

            totalSent += sentInBatch.Count;

            if (sentInBatch.Count == 0)
            {
                // No chunks were sent in this iteration, skip one and try next
                Logger.Warn($"[{Player.Username}] No chunks sent in batch iteration - payload too large?");
                _pending.Remove(_chunkQueue[start].Hash);
                start++;
            }
            else
            {
                start += sentInBatch.Count;
            }
        }

        if (start < count)
        {
            for (int i = start; i < count; i++)
            {
                _pending.Remove(_chunkQueue[i].Hash);
            }

            ReleaseQueuedChunks(start);
        }

        _chunkQueue.Clear();
        
        if (sendFailed)
        {
            return;
        }
    }

    private void QueueNextChunks(int limit)
    {
        if (Player.Dimension is null)
        {
            return;
        }

        int cx = FloorDiv((int)MathF.Floor(Player.Position.X), 16);
        int cz = FloorDiv((int)MathF.Floor(Player.Position.Z), 16);
        int[] offsets = GetRadialOffsets(ViewDistance);

        int skippedAlreadySent = 0;
        int skippedAlreadyPending = 0;
        int queued = 0;

        for (int i = 0; i < offsets.Length && _chunkQueue.Count < limit; i += 2)
        {
            int x = cx + offsets[i];
            int z = cz + offsets[i + 1];
            long hash = HashChunk(x, z);

            // Skip if already sent to client
            if (_chunks.Contains(hash))
            {
                skippedAlreadySent++;
                continue;
            }

            // Skip if already in pending or queue
            if (_pending.Contains(hash))
            {
                skippedAlreadyPending++;
                continue;
            }

            // Add to pending and queue for sending
            _pending.Add(hash);
            _chunkQueue.Add(Player.Dimension.GetOrCreateChunk(x, z));
            queued++;
        }

        // Log when chunks stop being queued
        if (queued == 0 && skippedAlreadySent + skippedAlreadyPending < offsets.Length / 2)
        {
            int totalNeeded = offsets.Length / 2;
            Logger.Warn($"[{Player.Username}] Chunk loading stalled at ({cx}, {cz}) - Sent: {_chunks.Count}/{totalNeeded}, Pending: {_pending.Count}, Queued: {_chunkQueue.Count}");
        }
    }

    private void RemoveOutOfRangeChunks()
    {
        if (_chunks.Count == 0)
        {
            return;
        }

        int cx = FloorDiv((int)MathF.Floor(Player.Position.X), 16);
        int cz = FloorDiv((int)MathF.Floor(Player.Position.Z), 16);
        float maxDistance = ViewDistance + 0.5f;
        float maxDistanceSquared = maxDistance * maxDistance;

        List<(int X, int Z)> toClear = [];
        foreach (long hash in _chunks)
        {
            UnhashChunk(hash, out int x, out int z);
            int dx = x - cx;
            int dz = z - cz;
            if ((dx * dx) + (dz * dz) > maxDistanceSquared)
            {
                toClear.Add((x, z));
            }
        }

        if (toClear.Count == 0)
        {
            return;
        }

        if (_emptyChunkData is null)
        {
            var empty = new ChunkColumn(0, 0, Player.Dimension!.Type);
            _emptyChunkData = ChunkColumn.Serialize(empty);
        }

        int unloadCount = Math.Min(Math.Max(24, GetChunkBatchSize() * 3), toClear.Count);
        List<DataPacket> packets = new(unloadCount);
        for (int i = 0; i < unloadCount; i++)
        {
            (int x, int z) = toClear[i];
            packets.Add(new LevelChunkPacket
            {
                ChunkX = x,
                ChunkZ = z,
                Dimension = (int)Player.Dimension!.Type,
                SubChunkCount = 0,
                CacheEnabled = false,
                RawPayload = _emptyChunkData
            });

            long hash = HashChunk(x, z);
            _chunks.Remove(hash);
            _pending.Remove(hash);
            if (Player.Dimension is not null)
            {
                Player.Dimension.RemoveChunkViewer(x, z);
                if (!Player.Dimension.HasChunkViewers(x, z))
                {
                    Player.Dimension.UnloadChunk(x, z);
                }
            }
        }

        for (int i = 0; i < packets.Count; i++)
        {
            Player.Send(packets[i]);
        }
    }

    private void Clear()
    {
        lock (_sync)
        {
            ReleaseTrackedChunks(tryUnload: true);
            ReleaseQueuedChunks(0);
            _chunks.Clear();
            _pending.Clear();
            _chunkQueue.Clear();
            _queueChunkX = int.MinValue;
            _queueChunkZ = int.MinValue;
        }
    }

    private void ReleaseTrackedChunks(bool tryUnload)
    {
        if (Player.Dimension is null || _chunks.Count == 0)
        {
            return;
        }

        foreach (long hash in _chunks)
        {
            UnhashChunk(hash, out int x, out int z);
            Player.Dimension.RemoveChunkViewer(x, z);
            if (tryUnload && !Player.Dimension.HasChunkViewers(x, z))
            {
                Player.Dimension.UnloadChunk(x, z);
            }
        }
    }

    private bool HandleChunkMovement(int currentChunkX, int currentChunkZ)
    {
        bool chunkChanged = currentChunkX != _queueChunkX || currentChunkZ != _queueChunkZ;
        if (!chunkChanged)
        {
            return false;
        }

        int dx = Math.Abs(currentChunkX - _queueChunkX);
        int dz = Math.Abs(currentChunkZ - _queueChunkZ);

        if (dx > 2 || dz > 2)
        {
            ReleaseQueuedChunks(0);
            _pending.Clear();
            _chunkQueue.Clear();
        }

        _queueChunkX = currentChunkX;
        _queueChunkZ = currentChunkZ;
        return true;
    }

    private NetworkChunkPublisherUpdatePacket CreateChunkPublisherUpdatePacket(bool includeSavedChunks)
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

        int chunkX = FloorDiv((int)MathF.Floor(Player.Position.X), 16);
        int chunkZ = FloorDiv((int)MathF.Floor(Player.Position.Z), 16);
        float maxDistance = ViewDistance + 0.5f;
        float maxDistanceSquared = maxDistance * maxDistance;
        packet.SavedChunks = new List<(int X, int Z)>(_chunks.Count);

        foreach (long hash in _chunks)
        {
            UnhashChunk(hash, out int x, out int z);
            int dx = x - chunkX;
            int dz = z - chunkZ;
            if ((dx * dx) + (dz * dz) <= maxDistanceSquared)
            {
                packet.SavedChunks.Add((x, z));
            }
        }

        return packet;
    }

    private void ReleaseQueuedChunks(int startIndex)
    {
        if (Player.Dimension is null || _chunkQueue.Count == 0)
        {
            return;
        }

        int start = Math.Clamp(startIndex, 0, _chunkQueue.Count);
        HashSet<long> processed = [];
        for (int i = start; i < _chunkQueue.Count; i++)
        {
            ChunkColumn chunk = _chunkQueue[i];
            long hash = chunk.Hash;
            if (!processed.Add(hash) || _chunks.Contains(hash))
            {
                continue;
            }

            if (Player.Dimension.HasChunkViewers(chunk.X, chunk.Z))
            {
                continue;
            }

            Player.Dimension.UnloadChunk(chunk.X, chunk.Z);
        }
    }

    private int[] GetRadialOffsets(int distance)
    {
        if (_offsetCache.TryGetValue(distance, out int[]? cached))
        {
            return cached;
        }

        float r2 = (distance + 0.5f) * (distance + 0.5f);
        List<(int Dist2, int X, int Z)> list = [];
        for (int dx = -distance; dx <= distance; dx++)
        {
            for (int dz = -distance; dz <= distance; dz++)
            {
                int dist2 = (dx * dx) + (dz * dz);
                if (dist2 <= r2)
                {
                    list.Add((dist2, dx, dz));
                }
            }
        }

        list.Sort(static (a, b) => a.Dist2.CompareTo(b.Dist2));
        int[] packed = new int[list.Count * 2];
        int index = 0;
        for (int i = 0; i < list.Count; i++)
        {
            packed[index++] = list[i].X;
            packed[index++] = list[i].Z;
        }

        _offsetCache[distance] = packed;
        return packed;
    }

    private int GetChunkBatchSize()
    {
        if (ViewDistance >= 24)
        {
            return 48;
        }
        if (ViewDistance >= 16)
        {
            return 32;
        }
        if (ViewDistance >= 8)
        {
            return 20;
        }
        return 12;
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
}
