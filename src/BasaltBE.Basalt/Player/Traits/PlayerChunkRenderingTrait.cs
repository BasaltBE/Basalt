using System.Diagnostics;
using System.Runtime.InteropServices;
using Basalt.Binary;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Network;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Basalt.Worlds.Types;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.Entities.Traits;

[EntityTraitAppliesTo(Identifiers = ["minecraft:player"])]
public sealed class PlayerChunkRenderingTrait : EntityTrait {
    const int ChunksPerTick = 64;
    const int PendingChecksPerTick = ChunksPerTick * 4;
    const int MovementPacketsPerBatch = 2048;
    const ulong MaximumOutgoingBufferedBytes = 16 * 1024;
    const int ChunkDataCapacity = 64 * 1024;
    const int MaximumChunkDataCapacity = 4 * 1024 * 1024 - 128;
    const float TntPositionChangeSquared = 0.0001f;
    const float TntRotationChangeSquared = 1f;
    const float TntMotionStopSquared = 0.0001f;
    [ThreadStatic]
    static ChunkSendResources? chunkSendResources;

    readonly Player player;
    readonly HashSet<long> loadedChunks = new(ChunkKeyComparer.Instance);
    readonly HashSet<long> requestedChunks = new(ChunkKeyComparer.Instance);
    readonly List<long> pendingChunks = [];
    readonly List<long> deferredChunks = [];
    readonly List<long> removedChunks = [];
    readonly Dictionary<ulong, EntityRenderState> renderedEntities = [];
    HashSet<ulong> visibleEntityIds = [];
    readonly List<ulong> removedEntities = [];
    readonly List<Entity> nearbyEntities = [];
    readonly List<DataPacket> movementPackets = new(MovementPacketsPerBatch);
    readonly Stack<MoveActorDeltaPacket> movementPacketPool = [];
    readonly Stack<SetActorMotionPacket> motionPacketPool = [];
    readonly Action<DataPacket> sendEntityPacket;
    ulong renderPass;
    readonly BlockPos publisherPosition = new();
    readonly NetworkChunkPublisherUpdatePacket publisherPacket = new();

    int spawnReady;
    int chunkX = int.MinValue;
    int chunkZ = int.MinValue;
    int viewDistance = -1;
    int ringRadius;
    int ringIndex;
    int pendingIndex;
    int publisherChunkX = int.MinValue;
    int publisherChunkZ = int.MinValue;
    int publisherViewDistance = -1;
    bool started;
    ulong chunkSendBlockedTick;

    public PlayerChunkRenderingTrait(Entity entity) : base(entity) {
        player = entity as Player ?? throw new ArgumentException("Player chunk rendering requires a Player entity.", nameof(entity));
        sendEntityPacket = packet => player.SendPacket(packet);
    }

    public override void OnSpawn() => Volatile.Write(ref spawnReady, 1);

    public bool Viewing(int chunkX, int chunkZ) => loadedChunks.Contains(HashChunk(chunkX, chunkZ));

    public bool Viewing(Entity entity) => Volatile.Read(ref visibleEntityIds).Contains(entity.RuntimeId);

    internal void RemoveEntity(Entity entity) {
        if (!renderedEntities.Remove(entity.RuntimeId)) return;
        Volatile.Write(ref visibleEntityIds, new HashSet<ulong>(renderedEntities.Keys));
        sendEntityPacket(new RemoveActorPacket { ActorId = entity.UniqueId });
    }

    public override void OnRemove() => OnDespawn();

    internal void UnloadChunks() {
        foreach (long hash in loadedChunks) {
            GetPosition(hash, out int x, out int z);
            SendUnload(x, z);
        }
    }

    public override void OnDespawn() {
        Volatile.Write(ref spawnReady, 0);
        started = false;
        foreach (long hash in loadedChunks) {
            GetPosition(hash, out int x, out int z);
            player.Dimension.RemoveChunkViewer(player, x, z);
        }

        loadedChunks.Clear();
        requestedChunks.Clear();
        pendingChunks.Clear();
        deferredChunks.Clear();
        removedChunks.Clear();
        renderedEntities.Clear();
        Volatile.Write(ref visibleEntityIds, []);
        removedEntities.Clear();
        nearbyEntities.Clear();
        pendingIndex = 0;
        ringRadius = 0;
        ringIndex = 0;
        chunkX = int.MinValue;
        chunkZ = int.MinValue;
        viewDistance = -1;
        publisherChunkX = int.MinValue;
        publisherChunkZ = int.MinValue;
        publisherViewDistance = -1;
    }

    public override void Tick() {
        if (Volatile.Read(ref spawnReady) == 0 || player.Disposed) return;

        int nextChunkX = (int)MathF.Floor(player.Position.X) >> 4;
        int nextChunkZ = (int)MathF.Floor(player.Position.Z) >> 4;
        int nextViewDistance = Math.Clamp(
            player.ChunkRadius,
            0,
            Math.Min(player.Network.Server.Properties.MaxViewDistance, ChunkViewMath.MaxBedrockViewDistance)
        );

        if (!started || chunkX != nextChunkX || chunkZ != nextChunkZ || viewDistance != nextViewDistance) {
            chunkX = nextChunkX;
            chunkZ = nextChunkZ;
            viewDistance = nextViewDistance;
            started = true;
            ResetRing();
            RemoveOutOfRangeChunks();
        }

        RetryDeferredChunks();
        RequestChunks();

        ReadOnlySpan<Entity> entities;
        if (player.Dimension.EntityCount > loadedChunks.Count * 2) {
            player.Dimension.EntityIndex.Collect(loadedChunks, nearbyEntities);
            entities = CollectionsMarshal.AsSpan(nearbyEntities);
        } else {
            entities = player.Dimension.GetEntities();
        }
        UpdateEntities(entities, sendEntityPacket);
    }

    internal void UpdateEntities(ReadOnlySpan<Entity> entities, Action<DataPacket> send) {
        renderPass++;
        bool membershipChanged = false;
        foreach (Entity entity in entities) {
            if (ReferenceEquals(entity, player) || entity.Type is null ||
                entity is Player { Spawned: false } or Player { Disposed: true } or Player { Dead: true } or Player { Listed: false } ||
                !float.IsFinite(entity.Position.X) || !float.IsFinite(entity.Position.Y) || !float.IsFinite(entity.Position.Z) ||
                !Viewing((int)MathF.Floor(entity.Position.X) >> 4, (int)MathF.Floor(entity.Position.Z) >> 4))
                continue;

            if (!renderedEntities.TryGetValue(entity.RuntimeId, out EntityRenderState? state)) {
                if (entity is Player other) {
                    PlayerInventoryTrait? inventory = other.GetTrait<PlayerInventoryTrait>();
                    SerializedAbilitiesData abilities = other.Abilities.CreateData(other.UniqueId, other.Permissions);
                    ActorDataItem[] flags = other.Flags.CreateSnapshot();
                    send(new AddPlayerPacket {
                        Uuid = other.ListEntry.AddEntry!.Uuid,
                        Name = other.Username,
                        RuntimeId = other.RuntimeId,
                        Position = other.Position,
                        Velocity = other.GetTrait<EntityMovementTrait>()?.Motion ?? new Vec3(),
                        Rotation = other.Rotation,
                        HeadRotation = other.HeadRotation,
                        GameType = other.GameType,
                        CarriedItem = inventory?.Inventory.GetItem(inventory.SelectedSlot)?.ToNetworkStackDescriptor() ?? new(),
                        ActorData = flags,
                        Abilities = abilities,
                        BuildPlatform = (BuildPlatform)other.DeviceOS
                    });
                    if (other.GetTrait<EntityArmorTrait>() is { } armor) send(armor.CreateEquipmentPacket());
                    send(new UpdateAbilitiesPacket { Data = abilities });
                    send(new SetActorDataPacket {
                        ActorRuntimeId = other.RuntimeId,
                        ActorData = flags,
                        Tick = other.GetTrait<EntityMovementTrait>()?.InputTick ?? 0
                    });
                } else if (entity is ItemEntity item) {
                    send(new AddItemActorPacket {
                        ActorId = item.UniqueId,
                        RuntimeId = item.RuntimeId,
                        Item = item.Item.ToNetworkStackDescriptor(),
                        Position = item.Position,
                        Velocity = item.GetTrait<EntityMovementTrait>()?.Motion ?? new Vec3(),
                        ActorData = item.Flags.CreateSnapshot()
                    });
                } else {
                    PrimedTntTrait? primedTnt = entity.GetTrait<PrimedTntTrait>();
                    Vec3 position = entity.Position;
                    if (primedTnt is not null)
                        position = new Vec3 { X = entity.Position.X, Y = entity.Position.Y + 0.5f, Z = entity.Position.Z };

                    send(new AddActorPacket {
                        ActorId = entity.UniqueId,
                        RuntimeId = entity.RuntimeId,
                        ActorType = entity.Type.Identifier,
                        Position = position,
                        Velocity = entity.GetTrait<EntityMovementTrait>()?.Motion ?? new Vec3(),
                        Rotation = entity.Rotation,
                        HeadRotation = entity.HeadRotation,
                        BodyRotation = entity.Rotation.Y,
                        Attributes = entity.Attributes.CreateSnapshot(),
                        ActorData = primedTnt is not null
                            ? [.. entity.Flags.CreateSnapshot(), primedTnt.CreateFuseTime()]
                            : entity switch {
                                FallingBlockEntity fallingBlock => [.. entity.Flags.CreateSnapshot(), fallingBlock.CreateBlockVariant()],
                                _ when entity.GetTrait<ExperienceOrbTrait>() is { } experienceOrb =>
                                    [.. entity.Flags.CreateSnapshot(), experienceOrb.CreateExperienceValue()],
                                _ => entity.Flags.CreateSnapshot()
                            }
                    });
                }
                state = new EntityRenderState(entity);
                renderedEntities.Add(entity.RuntimeId, state);
                membershipChanged = true;
            } else if (entity is not Player) {
                bool isTnt = entity.Type?.Identifier == "minecraft:tnt";
                int movementUpdateInterval = Math.Max(entity.MovementUpdateInterval, 1);
                if (state.TicksSinceMovementUpdate < movementUpdateInterval) state.TicksSinceMovementUpdate++;

                EntityMovementTrait? movement = entity.GetTrait<EntityMovementTrait>();
                float positionDeltaX = entity.Position.X - state.X;
                float positionDeltaY = entity.Position.Y - state.Y;
                float positionDeltaZ = entity.Position.Z - state.Z;
                float rotationDeltaX = entity.Rotation.X - state.Pitch;
                float rotationDeltaY = entity.Rotation.Y - state.Yaw;
                float headRotationDelta = entity.HeadRotation - state.HeadYaw;
                float positionDifference = positionDeltaX * positionDeltaX + positionDeltaY * positionDeltaY + positionDeltaZ * positionDeltaZ;
                float rotationDifference = rotationDeltaX * rotationDeltaX + rotationDeltaY * rotationDeltaY + headRotationDelta * headRotationDelta;
                bool positionChanged = isTnt
                    ? positionDifference > TntPositionChangeSquared
                    : positionDifference > 0;
                bool rotationChanged = isTnt
                    ? rotationDifference > TntRotationChangeSquared
                    : rotationDifference > 0;
                bool grounded = movement?.Grounded ?? false;
                bool movementChanged = positionChanged || rotationChanged || state.Grounded != grounded;
                bool movementUpdate = state.TicksSinceMovementUpdate >= movementUpdateInterval && (movementChanged || isTnt);

                if (movementUpdate) {
                    MoveActorDeltaPacket movementPacket = movementPacketPool.TryPop(out MoveActorDeltaPacket? pooledMovement)
                        ? pooledMovement
                        : new();
                    movementPacket.MoveData.ActorRuntimeId = entity.RuntimeId;
                    movementPacket.MoveData.PositionX = isTnt || state.X != entity.Position.X ? entity.Position.X : null;
                    movementPacket.MoveData.PositionY = isTnt || state.Y != entity.Position.Y
                        ? entity.Position.Y + (isTnt ? 0.5f : 0)
                        : null;
                    movementPacket.MoveData.PositionZ = isTnt || state.Z != entity.Position.Z ? entity.Position.Z : null;
                    movementPacket.MoveData.RotationX = state.Pitch != entity.Rotation.X
                        ? unchecked((sbyte)(entity.Rotation.X * 256 / 360))
                        : null;
                    movementPacket.MoveData.RotationY = state.Yaw != entity.Rotation.Y
                        ? unchecked((sbyte)(entity.Rotation.Y * 256 / 360))
                        : null;
                    movementPacket.MoveData.RotationYHead = state.HeadYaw != entity.HeadRotation
                        ? unchecked((sbyte)(entity.HeadRotation * 256 / 360))
                        : null;
                    movementPacket.MoveData.IsOnGround = movement?.Grounded == true;
                    movementPacket.MoveData.ForceMove = false;
                    movementPacket.MoveData.ForceMoveLocalEntity = false;
                    movementPacket.MoveData.ForceCompletion = false;
                    movementPacket.MoveData.Ticks = 0;
                    movementPackets.Add(movementPacket);
                    state.X = entity.Position.X;
                    state.Y = entity.Position.Y;
                    state.Z = entity.Position.Z;
                    state.Pitch = entity.Rotation.X;
                    state.Yaw = entity.Rotation.Y;
                    state.HeadYaw = entity.HeadRotation;
                    state.Grounded = movement?.Grounded ?? false;
                    state.TicksSinceMovementUpdate = 0;
                }

                if (movement is not null && (movementUpdate || isTnt)) {
                    float motionX = movement.Motion.X;
                    float motionY = movement.Motion.Y;
                    float motionZ = movement.Motion.Z;
                    float motionDeltaX = motionX - state.MotionX;
                    float motionDeltaY = motionY - state.MotionY;
                    float motionDeltaZ = motionZ - state.MotionZ;
                    float motionDifference = motionDeltaX * motionDeltaX + motionDeltaY * motionDeltaY + motionDeltaZ * motionDeltaZ;
                    float currentMotion = motionX * motionX + motionY * motionY + motionZ * motionZ;
                    bool motionStopped = isTnt && motionDifference > TntMotionStopSquared && currentMotion <= TntMotionStopSquared;
                    bool motionChanged = motionDifference > 1e-7f ||
                        (motionDifference > 0 && motionX == 0 && motionY == 0 && motionZ == 0);
                    if (isTnt ? movementUpdate || motionStopped : movementUpdate && motionChanged) {
                        SetActorMotionPacket motionPacket = motionPacketPool.TryPop(out SetActorMotionPacket? pooledMotion)
                            ? pooledMotion
                            : new();
                        motionPacket.TargetRuntimeId = entity.RuntimeId;
                        motionPacket.Motion.X = motionX;
                        motionPacket.Motion.Y = motionY;
                        motionPacket.Motion.Z = motionZ;
                        motionPacket.Tick = 0;
                        movementPackets.Add(motionPacket);
                        state.MotionX = motionX;
                        state.MotionY = motionY;
                        state.MotionZ = motionZ;
                    }
                }
            }
            state.Seen = renderPass;
            if (movementPackets.Count >= MovementPacketsPerBatch) FlushMovementPackets();
        }

        FlushMovementPackets();

        removedEntities.Clear();
        foreach ((ulong runtimeId, EntityRenderState state) in renderedEntities) {
            if (state.Seen != renderPass) {
                send(new RemoveActorPacket { ActorId = state.Entity.UniqueId });
                removedEntities.Add(runtimeId);
            }
        }
        foreach (ulong runtimeId in removedEntities) renderedEntities.Remove(runtimeId);
        if (removedEntities.Count > 0) {
            if (renderedEntities.Count == 0) renderedEntities.Clear();
            else if (removedEntities.Count > renderedEntities.Count) renderedEntities.TrimExcess();
        }
        if (membershipChanged || removedEntities.Count != 0)
            Volatile.Write(ref visibleEntityIds, new HashSet<ulong>(renderedEntities.Keys));
    }

    void FlushMovementPackets() {
        if (movementPackets.Count == 0) return;

        try {
            player.SendPacket(CollectionsMarshal.AsSpan(movementPackets), reliable: false);
        } finally {
            foreach (DataPacket packet in movementPackets) {
                if (packet is MoveActorDeltaPacket movement) movementPacketPool.Push(movement);
                else if (packet is SetActorMotionPacket motion) motionPacketPool.Push(motion);
            }
            movementPackets.Clear();
        }
    }

    void RetryDeferredChunks() {
        int retries = Math.Min(deferredChunks.Count, ChunksPerTick);
        for (int i = 0; i < deferredChunks.Count && retries > 0;) {
            long hash = deferredChunks[i];
            GetPosition(hash, out int x, out int z);
            if (!ChunkInRange(x, z)) {
                deferredChunks.RemoveAt(i);
                requestedChunks.Remove(hash);
                continue;
            }

            retries--;
            ChunkRequestStatus status = player.Dimension.Request(x, z);
            if (status == ChunkRequestStatus.QueueFull) {
                i++;
                continue;
            }

            deferredChunks.RemoveAt(i);
            pendingChunks.Add(hash);
        }
    }

    void RequestChunks() {
        int requests = 0;
        while (requests < ChunksPerTick && NextPosition(out int x, out int z)) {
            long hash = HashChunk(x, z);
            if (loadedChunks.Contains(hash) || !requestedChunks.Add(hash)) continue;

            requests++;
            if (player.Dimension.Request(x, z) == ChunkRequestStatus.QueueFull) {
                deferredChunks.Add(hash);
            } else {
                pendingChunks.Add(hash);
            }
        }
    }

    internal bool SendReadyChunk() {
        if (Volatile.Read(ref spawnReady) == 0 || player.Disposed || pendingChunks.Count == 0 ||
            chunkSendBlockedTick == player.Dimension.TickValue)
            return false;

        ulong outgoingBufferedBytes = player.Network.GetOutgoingBufferedBytes(player);
        if (outgoingBufferedBytes >= MaximumOutgoingBufferedBytes) {
            chunkSendBlockedTick = player.Dimension.TickValue;
            return false;
        }

        pendingIndex = 0;
        int checks = Math.Min(pendingChunks.Count, PendingChecksPerTick);
        while (checks > 0 && pendingChunks.Count > 0) {
            if (pendingIndex >= pendingChunks.Count) pendingIndex = 0;

            long hash = pendingChunks[pendingIndex];
            GetPosition(hash, out int x, out int z);
            checks--;

            if (!ChunkInRange(x, z)) {
                RemovePending(pendingIndex, hash);
                continue;
            }

            Chunk? chunk = player.Dimension.GetChunk(x, z);
            if (chunk is null) {
                pendingIndex = (pendingIndex + 1) % pendingChunks.Count;
                continue;
            }

            SendPublisherUpdate();

            long sendStart = Stopwatch.GetTimestamp();
            try {
                SendChunk(chunk);
            } finally {
                long elapsed = Stopwatch.GetTimestamp() - sendStart;
                if (player.Dimension.World is { } world) world.ChunkSendBudgetRemaining -= elapsed;
                else player.Dimension.ChunkSendBudgetRemaining -= elapsed;
            }
            loadedChunks.Add(hash);
            player.Dimension.AddChunkViewer(player, x, z);
            RemovePending(pendingIndex, hash);
            return true;
        }

        chunkSendBlockedTick = player.Dimension.TickValue;
        return false;
    }

    void SendPublisherUpdate() {
        if (publisherChunkX == chunkX && publisherChunkZ == chunkZ && publisherViewDistance == viewDistance) {
            return;
        }

        publisherPosition.X = (int)MathF.Floor(player.Position.X);
        publisherPosition.Y = (int)MathF.Floor(player.Position.Y);
        publisherPosition.Z = (int)MathF.Floor(player.Position.Z);
        publisherPacket.Position = publisherPosition;
        publisherPacket.Radius = ChunkViewMath.PublisherRadiusBlocks(viewDistance);
        player.SendPacket(publisherPacket);
        publisherChunkX = chunkX;
        publisherChunkZ = chunkZ;
        publisherViewDistance = viewDistance;
    }

    void RemoveOutOfRangeChunks() {
        removedChunks.Clear();
        foreach (long hash in loadedChunks) {
            GetPosition(hash, out int x, out int z);
            if (!ChunkInRange(x, z)) removedChunks.Add(hash);
        }

        foreach (long hash in removedChunks) {
            GetPosition(hash, out int x, out int z);
            SendUnload(x, z);
            loadedChunks.Remove(hash);
            player.Dimension.RemoveChunkViewer(player, x, z);
        }

        for (int i = pendingChunks.Count - 1; i >= 0; i--) {
            GetPosition(pendingChunks[i], out int x, out int z);
            if (ChunkInRange(x, z)) continue;

            requestedChunks.Remove(pendingChunks[i]);
            RemovePending(i, pendingChunks[i]);
        }

        for (int i = deferredChunks.Count - 1; i >= 0; i--) {
            GetPosition(deferredChunks[i], out int x, out int z);
            if (ChunkInRange(x, z)) continue;

            requestedChunks.Remove(deferredChunks[i]);
            deferredChunks.RemoveAt(i);
        }
    }

    void SendUnload(int x, int z) {
        ChunkSendResources resources = chunkSendResources ??= new();
        resources.Position.X = x;
        resources.Position.Z = z;
        resources.Packet.Position = resources.Position;
        resources.Packet.DimensionId = player.Dimension.RuntimeId;
        resources.Packet.SubChunkCount = 0;
        resources.Packet.ClientRequestSubChunkLimit = null;
        resources.Packet.CacheEnabled = false;
        resources.Packet.CacheMetadata = [];
        resources.Packet.SerializedChunkData = ReadOnlyMemory<byte>.Empty;
        resources.Packet.IsClientBiomeUpdate = false;
        player.SendPacket(resources.Packet);
    }

    void SendChunk(Chunk chunk) {
        ChunkSendResources resources = chunkSendResources ??= new();
        int minimumSubChunkY = player.Dimension.MinimumY >> 4;
        int dimensionSubChunkCount = player.Dimension.HeightRange >> 4;
        int subChunkCount = chunk.GetSubChunkSendCount(minimumSubChunkY, dimensionSubChunkCount);
        while (true) {
            resources.Stream.Offset = 0;
            BinaryWriter writer = resources.Stream.GetWriter();
            try {
                chunk.Serialize(
                    ref writer,
                    minimumSubChunkY,
                    subChunkCount,
                    dimensionSubChunkCount,
                    player.Dimension.CustomDefinition?.DefaultBiomeId ?? 0
                );
                break;
            } catch (ArgumentOutOfRangeException) {
                int capacity = resources.Stream.Buffer.Length;
                if (capacity >= MaximumChunkDataCapacity) {
                    throw new InvalidDataException("Chunk payload exceeds the maximum Bedrock packet size.");
                }

                ((IDisposable)resources.Stream).Dispose();
                resources.Stream = BinaryStream.Rent(Math.Min(capacity * 2, MaximumChunkDataCapacity));
            }
        }

        resources.Position.X = chunk.X;
        resources.Position.Z = chunk.Z;
        resources.Packet.Position = resources.Position;
        resources.Packet.DimensionId = player.Dimension.RuntimeId;
        resources.Packet.SubChunkCount = (uint)subChunkCount;
        resources.Packet.ClientRequestSubChunkLimit = null;
        resources.Packet.CacheEnabled = false;
        resources.Packet.CacheMetadata = [];
        resources.Packet.SerializedChunkData = resources.Stream.Buffer[..resources.Stream.Offset];
        resources.Packet.IsClientBiomeUpdate = false;
        try {
            player.SendPacket(resources.Packet);
        } finally {
            resources.Packet.SerializedChunkData = ReadOnlyMemory<byte>.Empty;
        }
    }

    void RemovePending(int index, long hash) {
        requestedChunks.Remove(hash);
        pendingChunks.RemoveAt(index);
        if (pendingChunks.Count == 0) {
            pendingIndex = 0;
        } else if (index < pendingIndex) {
            pendingIndex--;
        } else if (pendingIndex >= pendingChunks.Count) {
            pendingIndex = 0;
        }
    }

    bool NextPosition(out int x, out int z) {
        while (ringRadius <= viewDistance) {
            if (ringRadius == 0) {
                ringRadius = 1;
                ringIndex = 0;
                x = chunkX;
                z = chunkZ;
                return true;
            }

            int radius = ringRadius;
            int sideLength = radius * 2;
            int perimeterLength = radius * 8;
            if (ringIndex >= perimeterLength) {
                ringRadius++;
                ringIndex = 0;
                continue;
            }

            int index = ringIndex++;
            if (index < sideLength) {
                x = chunkX - radius + index;
                z = chunkZ - radius;
            } else if (index < sideLength * 2) {
                x = chunkX + radius;
                z = chunkZ - radius + index - sideLength;
            } else if (index < sideLength * 3) {
                x = chunkX + radius - (index - sideLength * 2);
                z = chunkZ + radius;
            } else {
                x = chunkX - radius;
                z = chunkZ + radius - (index - sideLength * 3);
            }

            return true;
        }

        x = 0;
        z = 0;
        return false;
    }

    void ResetRing() {
        ringRadius = 0;
        ringIndex = 0;
    }

    bool ChunkInRange(int x, int z) {
        long dx = (long)x - chunkX;
        long dz = (long)z - chunkZ;
        return Math.Max(Math.Abs(dx), Math.Abs(dz)) <= viewDistance;
    }

    static long HashChunk(int x, int z) => ((long)x << 32) | (uint)z;

    static void GetPosition(long hash, out int x, out int z) {
        x = (int)(hash >> 32);
        z = (int)hash;
    }

    sealed class ChunkSendResources {
        internal BinaryStream Stream = BinaryStream.Rent(ChunkDataCapacity);
        internal readonly ChunkPos Position = new();
        internal readonly LevelChunkPacket Packet = new();
    }
}
