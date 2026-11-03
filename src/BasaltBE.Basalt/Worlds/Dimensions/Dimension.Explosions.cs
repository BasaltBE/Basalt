using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Basalt.Blocks.Traits;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Basalt.LootTables;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using GameRule = BasaltBE.Basalt.Worlds.Types.GameRule;

namespace BasaltBE.Basalt.Worlds.Dimensions;

public sealed partial class Dimension {
    private static readonly (float X, float Y, float Z)[] ExplosionDirections = CreateExplosionDirections();
    private readonly HashSet<(int X, int Y, int Z)> affectedExplosionBlocks = [];
    private readonly Dictionary<(int X, int Y, int Z), List<UpdateSubChunkNetworkBlockInfo>> explosionUpdates = [];
    private readonly List<(int X, int Y, int Z)> explosionUpdateChunks = [];
    private readonly Dictionary<ItemType, long> explosionDrops = [];
    private readonly HashSet<(int X, int Y, int Z)> explosionNeighbors = [];
    private readonly List<(ItemStack Item, int X, int Y, int Z)> explosionContainerDrops = [];
    private readonly List<ItemEntity> explosionItemsToDespawn = [];

    private static (float X, float Y, float Z)[] CreateExplosionDirections() {
        (float X, float Y, float Z)[] directions = new (float X, float Y, float Z)[1352];
        int count = 0;
        for (int x = 0; x < 16; x++) {
            for (int y = 0; y < 16; y++) {
                for (int z = 0; z < 16; z++) {
                    if (x is not (0 or 15) && y is not (0 or 15) && z is not (0 or 15))
                        continue;

                    float directionX = x / 15f * 2 - 1;
                    float directionY = y / 15f * 2 - 1;
                    float directionZ = z / 15f * 2 - 1;
                    float length = MathF.Sqrt(directionX * directionX + directionY * directionY + directionZ * directionZ);
                    directions[count++] = (directionX / length, directionY / length, directionZ / length);
                }
            }
        }

        return directions;
    }

    internal void Explode(Entity tnt) {
        if (World?.GameRules.Get(GameRule.TntExplodes) == false)
            return;

        const float power = 4;
        const float radius = power * 2;
        float centerX = tnt.Position.X;
        float centerY = tnt.Position.Y + 0.49f;
        float centerZ = tnt.Position.Z;
        affectedExplosionBlocks.Clear();
        explosionDrops.Clear();
        explosionNeighbors.Clear();
        explosionItemsToDespawn.Clear();
        explosionContainerDrops.Clear();
        foreach ((int x, int y, int z) in explosionUpdateChunks)
            explosionUpdates[(x, y, z)].Clear();
        explosionUpdateChunks.Clear();

        foreach ((float directionX, float directionY, float directionZ) in ExplosionDirections) {
            float energy = power * (0.7f + Random.Shared.NextSingle() * 0.6f);
            float positionX = centerX;
            float positionY = centerY;
            float positionZ = centerZ;
            int previousBlockX = int.MinValue;
            int previousBlockY = int.MinValue;
            int previousBlockZ = int.MinValue;
            int currentChunkX = int.MinValue;
            int currentChunkZ = int.MinValue;
            int currentSubChunkY = int.MinValue;
            Chunk? currentChunk = null;
            SubChunk? currentSubChunk = null;
            float resistance = 0;
            bool hasBlock = false;
            while (energy > 0) {
                int blockX = (int)MathF.Floor(positionX);
                int blockY = (int)MathF.Floor(positionY);
                int blockZ = (int)MathF.Floor(positionZ);
                if (blockX != previousBlockX || blockY != previousBlockY || blockZ != previousBlockZ) {
                    previousBlockX = blockX;
                    previousBlockY = blockY;
                    previousBlockZ = blockZ;
                    int chunkX = blockX >> 4;
                    int chunkZ = blockZ >> 4;
                    if (chunkX != currentChunkX || chunkZ != currentChunkZ) {
                        currentChunkX = chunkX;
                        currentChunkZ = chunkZ;
                        currentChunk = GetChunk(chunkX, chunkZ);
                        currentSubChunkY = int.MinValue;
                    }

                    if (blockY < MinimumY || blockY > MaximumY || currentChunk is null)
                        break;

                    int subChunkY = blockY >> 4;
                    if (subChunkY != currentSubChunkY) {
                        currentSubChunkY = subChunkY;
                        currentSubChunk = currentChunk.GetSubChunk(subChunkY);
                    }

                    BlockPermutation block = currentSubChunk?.GetPermutationAt(
                        BlockStorage.GetIndex(blockX & 15, blockY & 15, blockZ & 15)
                    ) ?? BlockStorage.Air;
                    hasBlock = block.Identifier != "minecraft:air";
                    resistance = hasBlock ? BlockPalette.FindType(block.Identifier)?.BlastResistance ?? 0 : 0;
                    if (hasBlock && energy - (resistance + 0.3f) * 0.3f > 0)
                        affectedExplosionBlocks.Add((blockX, blockY, blockZ));
                }

                if (hasBlock)
                    energy -= (resistance + 0.3f) * 0.3f;

                positionX += directionX * 0.3f;
                positionY += directionY * 0.3f;
                positionZ += directionZ * 0.3f;
                energy -= 0.225f;
            }
        }

        foreach ((int x, int y, int z) in affectedExplosionBlocks) {
            if (GetChunk(x >> 4, z >> 4) is not { } chunk)
                continue;

            BlockPermutation block = chunk.GetPermutation(x & 15, y, z & 15);
            if (block.Identifier == "minecraft:air")
                continue;

            Container? inventory = GetBlockActor(chunk, x & 15, y, z & 15)?.Inventory;

            if (block.Identifier == "minecraft:tnt") {
                if (!TntTrait.Prime(
                    this, x, y, z, Random.Shared.Next(10, 31), broadcast: false, updateNeighbors: false
                ))
                    continue;
            }
            else if (!SetPermutation(x, y, z, BlockStorage.Air)) {
                continue;
            }
            if (inventory is not null) {
                for (int slot = 0; slot < inventory.ContainerSize; slot++) {
                    if (inventory.TakeItem(slot, ushort.MaxValue) is { } contents)
                        explosionContainerDrops.Add((contents, x, y, z));
                }
            }

            if (block.Identifier != "minecraft:tnt" && Random.Shared.NextSingle() < 1 / power &&
                World?.Server?.LootTables is { } lootTables) {
                foreach (BlockDrop drop in lootTables.Get(block.Identifier)) {
                    if (ItemPalette.Find(drop.Identifier) is not { } itemType ||
                        itemType.MaxAmount <= 0 || itemType.MaxAmount > ushort.MaxValue)
                        continue;

                    int amount = drop.MinAmount == drop.MaxAmount
                        ? drop.MinAmount
                        : (int)Random.Shared.NextInt64(drop.MinAmount, (long)drop.MaxAmount + 1);
                    if (amount > 0)
                        explosionDrops[itemType] = explosionDrops.GetValueOrDefault(itemType) + amount;
                }
            }

            var subChunk = (X: x >> 4, Y: y >> 4, Z: z >> 4);
            if (!explosionUpdates.TryGetValue(subChunk, out List<UpdateSubChunkNetworkBlockInfo>? blocks)) {
                blocks = [];
                explosionUpdates.Add(subChunk, blocks);
            }
            if (blocks.Count == 0)
                explosionUpdateChunks.Add(subChunk);
            blocks.Add(new UpdateSubChunkNetworkBlockInfo {
                Position = new BlockPos { X = x, Y = y, Z = z },
                RuntimeId = unchecked((uint)BlockStorage.Air.Hash),
                Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network
            });
        }

        foreach ((int chunkX, int subChunkY, int chunkZ) in explosionUpdateChunks) {
            List<UpdateSubChunkNetworkBlockInfo> blocks = explosionUpdates[(chunkX, subChunkY, chunkZ)];
            Broadcast(new UpdateSubChunkBlocksPacket {
                Position = new BlockPos { X = chunkX << 4, Y = subChunkY << 4, Z = chunkZ << 4 },
                BlocksChanged = new UpdateSubChunkBlocksChangedInfo { Standards = [.. blocks] }
            }, chunkX, chunkZ);

            foreach (UpdateSubChunkNetworkBlockInfo block in blocks) {
                int x = block.Position.X;
                int y = block.Position.Y;
                int z = block.Position.Z;
                explosionNeighbors.Add((x + 1, y, z));
                explosionNeighbors.Add((x - 1, y, z));
                explosionNeighbors.Add((x, y, z + 1));
                explosionNeighbors.Add((x, y, z - 1));
                explosionNeighbors.Add((x, y + 1, z));
                explosionNeighbors.Add((x, y - 1, z));
            }
        }

        foreach ((int x, int y, int z) in explosionNeighbors)
            UpdateNeighbor(x, y, z);

        Broadcast(new LevelEventPacket {
            EventId = (int)LevelEvent.ParticlesHugeExplosion,
            Position = new Vec3 { X = centerX, Y = centerY, Z = centerZ }
        }, new Vec3 { X = centerX, Y = centerY, Z = centerZ });
        Broadcast(new LevelSoundEventPacket {
            SoundEvent = "explode",
            Position = new Vec3 { X = centerX, Y = centerY, Z = centerZ },
            ActorIdentifier = "minecraft:tnt",
            ActorUniqueId = tnt.UniqueId
        }, new Vec3 { X = centerX, Y = centerY, Z = centerZ });

        int minimumCellX = (int)MathF.Floor((centerX - radius) / 8);
        int maximumCellX = (int)MathF.Floor((centerX + radius) / 8);
        int minimumCellZ = (int)MathF.Floor((centerZ - radius) / 8);
        int maximumCellZ = (int)MathF.Floor((centerZ + radius) / 8);
        for (int cellX = minimumCellX; cellX <= maximumCellX; cellX++) {
            for (int cellZ = minimumCellZ; cellZ <= maximumCellZ; cellZ++) {
                foreach (Entity entity in EntityIndex.GetCellEntities(cellX, cellZ)) {
                    if (entity.RuntimeId == tnt.RuntimeId || entity.Dead ||
                        entity is Player { GameType: GameType.Creative or GameType.Spectator })
                        continue;

                    float dx = entity.Position.X - centerX;
                    bool primedTnt = entity.Type?.Identifier == "minecraft:tnt";
                    float dy = entity.Position.Y + (primedTnt ? 0.49f : 0.9f) - centerY;
                    float dz = entity.Position.Z - centerZ;
                    float distanceSquared = dx * dx + dy * dy + dz * dz;
                    if (distanceSquared > radius * radius)
                        continue;
                    if (distanceSquared == 0 && primedTnt) {
                        float angle = Random.Shared.NextSingle() * (MathF.PI * 2);
                        dx = MathF.Cos(angle);
                        dy = 0.35f;
                        dz = MathF.Sin(angle);
                        distanceSquared = dx * dx + dy * dy + dz * dz;
                    }
                    if (distanceSquared == 0)
                        continue;
                    float distance = MathF.Sqrt(distanceSquared);

                    int sampleCount = (entity is ItemEntity || primedTnt) ||
                        entity.GetTrait<ExperienceOrbTrait>() is not null ? 1 : 3;
                    float visibleSamples = 0;
                    for (int sampleX = 0; sampleX < sampleCount; sampleX++) {
                        for (int sampleY = 0; sampleY < sampleCount; sampleY++) {
                            for (int sampleZ = 0; sampleZ < sampleCount; sampleZ++) {
                                float targetX = entity.Position.X + (sampleX - (sampleCount - 1) * 0.5f) * 0.3f;
                                float targetY = sampleCount == 1
                                    ? entity.Position.Y + 0.49f
                                    : entity.Position.Y + sampleY * 0.9f;
                                float targetZ = entity.Position.Z + (sampleZ - (sampleCount - 1) * 0.5f) * 0.3f;
                                float rayX = targetX - centerX;
                                float rayY = targetY - centerY;
                                float rayZ = targetZ - centerZ;
                                float rayDistance = sampleCount == 1
                                    ? distance
                                    : MathF.Sqrt(rayX * rayX + rayY * rayY + rayZ * rayZ);
                                int raySteps = Math.Max(1, (int)(rayDistance * 3));
                                bool visible = true;
                                int previousBlockX = int.MinValue;
                                int previousBlockY = int.MinValue;
                                int previousBlockZ = int.MinValue;
                                int currentChunkX = int.MinValue;
                                int currentChunkZ = int.MinValue;
                                Chunk? currentChunk = null;
                                bool solid = false;
                                for (int step = 1; step < raySteps; step++) {
                                    float fraction = (float)step / raySteps;
                                    int blockX = (int)MathF.Floor(centerX + rayX * fraction);
                                    int blockY = (int)MathF.Floor(centerY + rayY * fraction);
                                    int blockZ = (int)MathF.Floor(centerZ + rayZ * fraction);
                                    if (blockX != previousBlockX || blockY != previousBlockY || blockZ != previousBlockZ) {
                                        previousBlockX = blockX;
                                        previousBlockY = blockY;
                                        previousBlockZ = blockZ;
                                        int rayChunkX = blockX >> 4;
                                        int rayChunkZ = blockZ >> 4;
                                        if (rayChunkX != currentChunkX || rayChunkZ != currentChunkZ) {
                                            currentChunkX = rayChunkX;
                                            currentChunkZ = rayChunkZ;
                                            currentChunk = GetChunk(rayChunkX, rayChunkZ);
                                        }

                                        if (blockY >= MinimumY && blockY <= MaximumY && currentChunk is not null) {
                                            solid = BlockPalette.FindType(
                                                currentChunk.GetPermutation(blockX & 15, blockY, blockZ & 15).Identifier
                                            )?.Solid == true;
                                        }
                                        else {
                                            solid = false;
                                        }
                                    }

                                    if (!solid)
                                        continue;

                                    visible = false;
                                    break;
                                }

                                if (visible)
                                    visibleSamples++;
                            }
                        }
                    }

                    float exposure = visibleSamples / (sampleCount * sampleCount * sampleCount);
                    float impact = (1 - distance / radius) * exposure;
                    if (impact <= 0)
                        continue;

                    if (entity is ItemEntity item) {
                        explosionItemsToDespawn.Add(item);
                        continue;
                    }

                    entity.GetTrait<EntityHealthTrait>()?.ApplyDamage((impact * impact + impact) * 0.5f * 7 * power + 1, EntityDamageCause.Explosion, tnt);
                    if (entity.GetTrait<EntityMovementTrait>() is not { } movement)
                        continue;

                    float inverseDistance = 1 / distance;
                    SetActorMotionPacket motion = new() {
                        TargetRuntimeId = entity.RuntimeId,
                        Motion = new Vec3 {
                            X = movement.Motion.X + dx * inverseDistance * impact,
                            Y = movement.Motion.Y + dy * inverseDistance * impact,
                            Z = movement.Motion.Z + dz * inverseDistance * impact
                        },
                        Tick = entity is Player
                            ? movement.InputTick ?? 0
                            : 0
                    };
                    if (entity is Player player) {
                        Broadcast(motion, entity, exclude: player);
                        player.SendPacket(motion);
                    }
                    else {
                        movement.Motion.X = motion.Motion.X;
                        movement.Motion.Y = motion.Motion.Y;
                        movement.Motion.Z = motion.Motion.Z;
                        Broadcast(motion, entity);
                    }
                }
            }
        }

        foreach (ItemEntity item in explosionItemsToDespawn) {
            Broadcast(new RemoveActorPacket { ActorId = item.UniqueId }, item);
            RemoveEntity(item);
        }

        foreach ((ItemStack item, int x, int y, int z) in explosionContainerDrops)
            new ItemEntity(item, x, y, z).Spawn(this);

        foreach ((ItemType itemType, long amount) in explosionDrops) {
            long remaining = amount;
            while (remaining > 0) {
                ushort stackSize = (ushort)Math.Min(itemType.MaxAmount, remaining);
                ItemEntity item = new(
                    new ItemStack(itemType, stackSize),
                    (int)MathF.Floor(centerX),
                    (int)MathF.Floor(centerY),
                    (int)MathF.Floor(centerZ)
                );
                item.Spawn(this);
                remaining -= stackSize;
            }
        }
    }
}
