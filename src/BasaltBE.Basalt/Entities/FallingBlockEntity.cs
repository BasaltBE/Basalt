using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Entities.Types.Components;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Entities;

internal sealed class FallingBlockEntity : Entity {
    private static readonly EntityType FallingBlockType = new() {
        Identifier = "minecraft:falling_block",
        PropertiesPayload = new EntityTypePayload {
            Components = {
                CollisionBox = new EntityCollisionBoxComponent { Width = 0.98, Height = 0.98 }
            }
        }
    };
    private readonly BlockPermutation Block;
    private readonly EntityMovementTrait movement;
    private float fallStart;

    internal FallingBlockEntity(BlockPermutation block, float x, float y, float z) : base(FallingBlockType) {
        Block = block;
        Position.X = x;
        Position.Y = y;
        Position.Z = z;
        fallStart = y;
        SetFlag(ActorFlag.HasGravity, true);
        movement = new(this) { Gravity = 0.04f, Drag = 0.02f };
        AddTrait(movement);
        AddTrait(new LandingTrait(this));
    }

    public override void Read(CompoundTag tag) {
        base.Read(tag);
        float distance = tag.Get<FloatTag>("FallDistance")?.Value ?? 0;
        fallStart = Position.Y + distance;
        if (!float.IsFinite(distance) || distance < 0 || !float.IsFinite(fallStart))
            throw new InvalidDataException("Falling block start height is invalid.");
    }

    public override void Write(CompoundTag tag) {
        base.Write(tag);
        tag.Add("Block", BlockStorage.CreatePermutationTag(Block));
        tag.Add("FallDistance", new FloatTag { Value = Math.Max(0, fallStart - Position.Y) });
    }

    private void Land(Dimension dimension) {
        int x = (int)MathF.Floor(Position.X);
        int y = (int)MathF.Floor(Position.Y);
        int z = (int)MathF.Floor(Position.Z);
        if (dimension.GetChunk(x >> 4, z >> 4) is null)
            return;

        if (BlockPalette.FindType(dimension.GetPermutation(x, y, z).Identifier)?.Solid == true) {
            new ItemEntity(new ItemStack("minecraft:anvil"), this).Spawn(dimension);
            dimension.Broadcast(new RemoveActorPacket { ActorId = UniqueId }, this);
            Despawn();
            return;
        }

        float distance = Math.Max(0, fallStart - Position.Y);
        double damageChance = 0.05 + (MathF.Floor(distance + 0.5f) - 1) * 0.05;
        BlockPermutation landedBlock = Block;
        if (Random.Shared.NextDouble() < damageChance) {
            if (AnvilContainer.AdvanceDamage(Block) is not { } damaged) {
                new ItemEntity(new ItemStack("minecraft:anvil"), this).Spawn(dimension);
                dimension.Broadcast(new RemoveActorPacket { ActorId = UniqueId }, this);
                Despawn();
                return;
            }
            landedBlock = damaged;
        }

        dimension.SetPermutation(x, y, z, landedBlock);
        dimension.Broadcast(new UpdateBlockPacket {
            Position = new BlockPos { X = x, Y = y, Z = z },
            BlockRuntimeId = unchecked((uint)landedBlock.Hash),
            Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network
        }, x >> 4, z >> 4, additionalPacket: new LevelSoundEventPacket {
            SoundEvent = "random.anvil_land",
            Position = new Vec3 { X = x + 0.5f, Y = y + 0.5f, Z = z + 0.5f },
            ActorUniqueId = -1
        });
        dimension.UpdateNeighbors(x, y, z);
        dimension.Broadcast(new RemoveActorPacket { ActorId = UniqueId }, this);
        Despawn();
    }

    private sealed class LandingTrait(FallingBlockEntity entity) : EntityTrait(entity) {
        public override void Tick() {
            if (entity.movement.Grounded && entity.SpawnedDimension is { } dimension)
                entity.Land(dimension);
        }
    }

    internal ActorDataItem CreateBlockVariant() => new() {
        Id = (uint)ActorDataId.Variant,
        Type = DataItemType.Int,
        Value = Block.NetworkId
    };
}
