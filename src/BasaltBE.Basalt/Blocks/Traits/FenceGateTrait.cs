using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo(
    "minecraft:fence_gate",
    "minecraft:acacia_fence_gate",
    "minecraft:bamboo_fence_gate",
    "minecraft:birch_fence_gate",
    "minecraft:cherry_fence_gate",
    "minecraft:dark_oak_fence_gate",
    "minecraft:jungle_fence_gate",
    "minecraft:mangrove_fence_gate",
    "minecraft:pale_oak_fence_gate",
    "minecraft:poplar_fence_gate",
    "minecraft:spruce_fence_gate",
    "minecraft:crimson_fence_gate",
    "minecraft:warped_fence_gate"
)]
public sealed class FenceGateTrait : BlockTrait {
    public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) {
        BlockPermutation current = dimension.GetPermutation(x, y, z);
        if (current.State.Values.GetValueOrDefault("minecraft:cardinal_direction") is not string direction ||
            current.State.Values.GetValueOrDefault("open_bit") is not bool open)
            return false;

        bool inWall = direction is "north" or "south"
            ? BlockPalette.IsWall(dimension.GetPermutation(x - 1, y, z).Identifier) &&
                BlockPalette.IsWall(dimension.GetPermutation(x + 1, y, z).Identifier)
            : BlockPalette.IsWall(dimension.GetPermutation(x, y, z - 1).Identifier) &&
                BlockPalette.IsWall(dimension.GetPermutation(x, y, z + 1).Identifier);
        BlockPermutation? updated = BlockPalette.FindFenceGatePermutation(current.Identifier, direction, open, inWall);
        if (updated is null || updated == current) return false;

        dimension.SetPermutation(x, y, z, updated);
        return true;
    }

    public override bool OnInteract(Player player, BlockPos position) {
        BlockPermutation current = player.Dimension.GetPermutation(position.X, position.Y, position.Z);
        if (!BlockPalette.IsFenceGate(current.Identifier) ||
            current.State.Values.GetValueOrDefault("minecraft:cardinal_direction") is not string direction ||
            current.State.Values.GetValueOrDefault("open_bit") is not bool open ||
            current.State.Values.GetValueOrDefault("in_wall_bit") is not bool inWall ||
            BlockPalette.FindFenceGatePermutation(current.Identifier, direction, !open, inWall) is not { } updated)
            return false;

        player.Dimension.SetPermutation(position.X, position.Y, position.Z, updated);
        player.Dimension.Broadcast(new UpdateBlockPacket {
            Position = position,
            BlockRuntimeId = unchecked((uint)updated.Hash),
            Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network
        }, position.X >> 4, position.Z >> 4, additionalPacket: new LevelSoundEventPacket {
            SoundEvent = open ? "fence_gate.close" : "fence_gate.open",
            Position = new Vec3 { X = position.X + 0.5f, Y = position.Y + 0.5f, Z = position.Z + 0.5f },
            ActorUniqueId = -1
        });
        return true;
    }
}
