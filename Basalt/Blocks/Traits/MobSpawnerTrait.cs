namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Player;
using Basalt.Core.Worlds.Dimensions;

using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public sealed class MobSpawnerTrait : BlockTrait {
    public static new readonly string Identifier = "mob_spawner";
    public static new readonly string[] Types = ["minecraft:mob_spawner"];

    public int BlockEntityVersion;
    public short Delay = 20;
    public float DisplayEntityHeight = 1.8f;
    public float DisplayEntityScale = 1f;
    public float DisplayEntityWidth = 0.8f;
    public string EntityIdentifier = "minecraft:zombie";
    public short MaxNearbyEntities = 6;
    public short MaxSpawnDelay = 800;
    public short MinSpawnDelay = 200;
    public short RequiredPlayerRange = 16;
    public short SpawnCount = 4;
    public short SpawnRange = 4;

    private bool _ticking;
    private readonly List<Entity> _nearbyEntities = [];

    public MobSpawnerTrait(Block block) : base(block) {
    }

    public override void OnRead(CompoundTag tag) {
        BlockEntityVersion = tag.Get<IntTag>("BlockEntityVersion")?.Value ?? BlockEntityVersion;
        Delay = tag.Get<ShortTag>("Delay")?.Value ?? Delay;
        DisplayEntityHeight = tag.Get<FloatTag>("DisplayEntityHeight")?.Value ?? DisplayEntityHeight;
        DisplayEntityScale = tag.Get<FloatTag>("DisplayEntityScale")?.Value ?? DisplayEntityScale;
        DisplayEntityWidth = tag.Get<FloatTag>("DisplayEntityWidth")?.Value ?? DisplayEntityWidth;
        EntityIdentifier = tag.Get<StringTag>("EntityIdentifier")?.Value ?? EntityIdentifier;
        MaxNearbyEntities = tag.Get<ShortTag>("MaxNearbyEntities")?.Value ?? MaxNearbyEntities;
        MaxSpawnDelay = tag.Get<ShortTag>("MaxSpawnDelay")?.Value ?? MaxSpawnDelay;
        MinSpawnDelay = tag.Get<ShortTag>("MinSpawnDelay")?.Value ?? MinSpawnDelay;
        RequiredPlayerRange = tag.Get<ShortTag>("RequiredPlayerRange")?.Value ?? RequiredPlayerRange;
        SpawnCount = tag.Get<ShortTag>("SpawnCount")?.Value ?? SpawnCount;
        SpawnRange = tag.Get<ShortTag>("SpawnRange")?.Value ?? SpawnRange;
    }

    public override void OnWrite(CompoundTag tag) {
        tag.Set("BlockEntityVersion", new IntTag { Value = BlockEntityVersion });
        tag.Set("Delay", new ShortTag { Value = Delay });
        tag.Set("DisplayEntityHeight", new FloatTag { Value = DisplayEntityHeight });
        tag.Set("DisplayEntityScale", new FloatTag { Value = DisplayEntityScale });
        tag.Set("DisplayEntityWidth", new FloatTag { Value = DisplayEntityWidth });
        tag.Set("EntityIdentifier", new StringTag { Value = EntityIdentifier });
        tag.Set("MaxNearbyEntities", new ShortTag { Value = MaxNearbyEntities });
        tag.Set("MaxSpawnDelay", new ShortTag { Value = MaxSpawnDelay });
        tag.Set("MinSpawnDelay", new ShortTag { Value = MinSpawnDelay });
        tag.Set("RequiredPlayerRange", new ShortTag { Value = RequiredPlayerRange });
        tag.Set("SpawnCount", new ShortTag { Value = SpawnCount });
        tag.Set("SpawnRange", new ShortTag { Value = SpawnRange });
    }

    public override void OnPlace(BlockPlaceDetails details) {
        Dimension? dimension = details.Player.Dimension;
        if (dimension is null) {
            return;
        }

        WriteStorage(dimension, details.BlockPosition);
        BroadcastUpdate(dimension, details.BlockPosition, refreshBlock: true);
        ScheduleTick(dimension, details.BlockPosition);
    }

    public override void OnRender(Player player, int x, int y, int z) {
        Dimension? dimension = player.Dimension;
        if (dimension is null) {
            return;
        }

        BlockPos position = new() { X = x, Y = y, Z = z };
        WriteStorage(dimension, position);
        ScheduleTick(dimension, position);

        BlockLevelStorage? storage = dimension
            .GetChunk(x >> 4, z >> 4)
            ?.GetBlockStorage(position);

        if (storage is null) {
            return;
        }

        uint networkId = (uint)dimension.GetPermutation(x, y, z).NetworkId;

        player.Send(
            new BlockActorDataPacket {
                Position = position,
                ActorData = storage,
                
            },
            new UpdateBlockPacket {
                Position = position,
                BlockRuntimeId = 0,
                Flags = (uint)UpdateBlockFlagsType.None,
                Layer = (uint)UpdateBlockLayerType.Normal
            },
            new UpdateBlockPacket {
                Position = position,
                BlockRuntimeId = networkId,
                Flags = (uint)UpdateBlockFlagsType.None,
                Layer = (uint)UpdateBlockLayerType.Normal
            });
    }

    public bool Tick(Dimension dimension, BlockPos position) {
        if (!PlayerInRange(dimension, position)) {
            return false;
        }

        if (Delay < 0) {
            ResetDelay();
        }

        if (Delay > 0) {
            Delay--;
            return true;
        }

        if (string.IsNullOrWhiteSpace(EntityIdentifier) ||
            EntityType.Get(EntityIdentifier) is null ||
            SpawnCount <= 0) {
            return true;
        }

        bool spawned = false;
        int spawnRange = Math.Max(0, (int)SpawnRange);
        int nearbyEntities = NearbyEntityCount(dimension, position, spawnRange);

        for (int i = 0; i < SpawnCount; i++) {
            if (nearbyEntities >= Math.Max(0, (int)MaxNearbyEntities)) {
                ResetDelay();
                WriteStorage(dimension, position);
                BroadcastUpdate(dimension, position);
                return true;
            }

            Vec3 spawnPosition = new() {
                X = position.X + 0.5f + RandomOffset(spawnRange),
                Y = position.Y + Random.Shared.Next(-1, 2),
                Z = position.Z + 0.5f + RandomOffset(spawnRange)
            };

            Entity entity = new(EntityIdentifier) {
                Position = spawnPosition
            };

            if (!HasSpawnSpace(dimension, entity, spawnPosition)) {
                continue;
            }

            entity.Spawn(dimension, new EntitySpawnOptions(InitialSpawn: false));
            spawned = true;
            nearbyEntities++;
        }

        if (spawned) {
            ResetDelay();
            WriteStorage(dimension, position);
            BroadcastUpdate(dimension, position);
        }

        return true;
    }

    public void Configure(Dimension dimension, BlockPos position, string entityIdentifier) {
        EntityIdentifier = entityIdentifier;
        WriteStorage(dimension, position);
        BroadcastUpdate(dimension, position, refreshBlock: true);
    }

    private static void BroadcastUpdate(Dimension dimension, BlockPos position, bool refreshBlock = false) {
        BlockLevelStorage? storage = dimension
            .GetChunk(position.X >> 4, position.Z >> 4)
            ?.GetBlockStorage(position);

        if (storage is null) {
            return;
        }

        dimension.Broadcast(new BlockActorDataPacket {
            Position = position,
            ActorData = storage,
            
        });

        if (!refreshBlock) {
            return;
        }

        uint networkId = (uint)dimension.GetPermutation(position.X, position.Y, position.Z).NetworkId;
        dimension.Broadcast(new UpdateBlockPacket {
            Position = position,
            BlockRuntimeId = 0,
            Flags = (uint)UpdateBlockFlagsType.None,
            Layer = (uint)UpdateBlockLayerType.Normal
        });
        dimension.Broadcast(new UpdateBlockPacket {
            Position = position,
            BlockRuntimeId = networkId,
            Flags = (uint)UpdateBlockFlagsType.None,
            Layer = (uint)UpdateBlockLayerType.Normal
        });
    }

    private void ScheduleTick(Dimension dimension, BlockPos position) {
        if (_ticking || dimension.World?.Scheduler is null) {
            return;
        }

        _ticking = true;
        dimension.World.Scheduler.Schedule(new MobSpawnerTickTask(dimension, position));
    }

    private bool PlayerInRange(Dimension dimension, BlockPos position) {
        float range = Math.Max(0, (int)RequiredPlayerRange);
        float rangeSquared = range * range;
        float centerX = position.X + 0.5f;
        float centerY = position.Y + 0.5f;
        float centerZ = position.Z + 0.5f;

        _nearbyEntities.Clear();
        GatherEntities(dimension, centerX, centerZ, range, _nearbyEntities);
        foreach (Entity entity in _nearbyEntities) {
            if (entity is not Player player || !player.Spawned || player.Dimension != dimension) {
                continue;
            }

            float dx = player.Position.X - centerX;
            float dy = player.Position.Y - centerY;
            float dz = player.Position.Z - centerZ;
            if ((dx * dx) + (dy * dy) + (dz * dz) <= rangeSquared) {
                return true;
            }
        }

        return false;
    }

    private int NearbyEntityCount(Dimension dimension, BlockPos position, int spawnRange) {
        int count = 0;
        float centerX = position.X + 0.5f;
        float centerY = position.Y + 0.5f;
        float centerZ = position.Z + 0.5f;

        _nearbyEntities.Clear();
        GatherEntities(dimension, centerX, centerZ, Math.Max(spawnRange, 4), _nearbyEntities);
        foreach (Entity entity in _nearbyEntities) {
            if (!entity.IsAlive ||
                entity.PendingDespawn ||
                entity.Dimension != dimension ||
                !string.Equals(entity.Identifier, EntityIdentifier, StringComparison.Ordinal)) {
                continue;
            }

            if (MathF.Abs(entity.Position.X - centerX) <= spawnRange &&
                MathF.Abs(entity.Position.Y - centerY) <= 4f &&
                MathF.Abs(entity.Position.Z - centerZ) <= spawnRange) {
                count++;
            }
        }

        return count;
    }

    private static void GatherEntities(Dimension dimension, float centerX, float centerZ, float range, List<Entity> entities) {
        int minChunkX = (int)MathF.Floor((centerX - range) / 16f);
        int maxChunkX = (int)MathF.Floor((centerX + range) / 16f);
        int minChunkZ = (int)MathF.Floor((centerZ - range) / 16f);
        int maxChunkZ = (int)MathF.Floor((centerZ + range) / 16f);

        for (int chunkX = minChunkX; chunkX <= maxChunkX; chunkX++) {
            for (int chunkZ = minChunkZ; chunkZ <= maxChunkZ; chunkZ++) {
                entities.AddRange(dimension.GetEntities(chunkX, chunkZ));
            }
        }
    }

    private static bool HasSpawnSpace(Dimension dimension, Entity entity, Vec3 position) {
        EntityCollisionTrait? collision = entity.GetTrait<EntityCollisionTrait>();
        float width = collision?.Width ?? EntityCollisionTrait.DefaultWidth;
        float height = collision?.Height ?? EntityCollisionTrait.DefaultHeight;
        float halfWidth = width * 0.5f;

        int minX = (int)MathF.Floor(position.X - halfWidth);
        int maxX = (int)MathF.Floor(position.X + halfWidth);
        int minY = (int)MathF.Floor(position.Y);
        int maxY = (int)MathF.Floor(position.Y + height);
        int minZ = (int)MathF.Floor(position.Z - halfWidth);
        int maxZ = (int)MathF.Floor(position.Z + halfWidth);

        for (int x = minX; x <= maxX; x++) {
            for (int y = minY; y <= maxY; y++) {
                for (int z = minZ; z <= maxZ; z++) {
                    if (dimension.GetPermutation(x, y, z).Type.Solid) {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static float RandomOffset(int range) {
        return ((float)Random.Shared.NextDouble() - (float)Random.Shared.NextDouble()) * range;
    }

    private void ResetDelay() {
        int min = Math.Max(0, (int)MinSpawnDelay);
        int max = Math.Max(min, (int)MaxSpawnDelay);
        Delay = (short)Random.Shared.Next(min, max + 1);
    }

    private void WriteStorage(Dimension dimension, BlockPos position) {
        var chunk = dimension.GetChunk(position.X >> 4, position.Z >> 4);
        if (chunk is null) {
            return;
        }

        BlockLevelStorage? storage = chunk.GetBlockStorage(position);
        if (storage is null) {
            storage = new BlockLevelStorage(chunk);
            storage.SetPosition(position);
        }

        storage.Set("id", new StringTag { Value = "MobSpawner" });
        OnWrite(storage);
        chunk.SetBlockStorage(position, storage, dirty: true);
    }
}
