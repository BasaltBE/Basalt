using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

public sealed class DoorTrait : BlockSupportTrait {
    public override bool OnPlace(Player player, int x, int y, int z, byte face, Vec3 clickPosition, ref BlockPermutation permutation) {
        Dimension dimension = player.Dimension;
        if (face != 1 || y >= dimension.MaximumY || dimension.GetChunk(x >> 4, z >> 4) is null ||
            !BlockPalette.Replaceable(dimension.GetPermutation(x, y + 1, z)) ||
            BlockPalette.FindType(dimension.GetPermutation(x, y - 1, z).Identifier)?.Solid != true)
            return false;

        int facing = (int)MathF.Floor((player.Rotation.Y + 45) / 90) & 3;
        string direction = BlockDirections.ByYaw[(facing + 1) & 3];
        permutation = BlockPalette.Find(permutation.Identifier, permutation.State, "minecraft:cardinal_direction", direction) ?? permutation;

        (int leftX, int leftZ) = BlockDirections.LeftByYaw[facing];
        BlockPermutation left = dimension.GetPermutation(x + leftX, y, z + leftZ);
        BlockPermutation right = dimension.GetPermutation(x - leftX, y, z - leftZ);
        bool leftTransparent = (BlockPalette.FindType(left.Identifier)?.Opacity ?? 0) < 15;
        bool rightOpaque = (BlockPalette.FindType(right.Identifier)?.Opacity ?? 0) >= 15;
        bool hingeRight = left.Identifier == permutation.Identifier || rightOpaque && leftTransparent;

        permutation = BlockPalette.Find(permutation.Identifier, permutation.State, "door_hinge_bit", hingeRight) ?? permutation;
        permutation = BlockPalette.Find(permutation.Identifier, permutation.State, "upper_block_bit", false) ?? permutation;
        permutation = BlockPalette.Find(permutation.Identifier, permutation.State, "open_bit", false) ?? permutation;
        if (BlockPalette.Find(permutation.Identifier, permutation.State, "upper_block_bit", true) is not { } upper) return false;

        dimension.SetPermutation(x, y + 1, z, upper);
        return true;
    }

    public override bool OnInteract(Player player, BlockPos position) {
        Dimension dimension = player.Dimension;
        BlockPermutation clicked = dimension.GetPermutation(position.X, position.Y, position.Z);
        if (!BlockPalette.IsDoor(clicked.Identifier) || clicked.Identifier == "minecraft:iron_door")
            return BlockPalette.IsDoor(clicked.Identifier);

        bool upper = clicked.State.Values.GetValueOrDefault("upper_block_bit") is true;
        int lowerY = position.Y - (upper ? 1 : 0);
        int upperY = lowerY + 1;
        BlockPermutation lower = dimension.GetPermutation(position.X, lowerY, position.Z);
        BlockPermutation top = dimension.GetPermutation(position.X, upperY, position.Z);
        if (lower.Identifier != clicked.Identifier || top.Identifier != clicked.Identifier ||
            lower.State.Values.GetValueOrDefault("upper_block_bit") is true ||
            top.State.Values.GetValueOrDefault("upper_block_bit") is not true)
            return true;

        if (lower.State.Values.GetValueOrDefault("open_bit") is not bool open ||
            BlockPalette.Find(lower.Identifier, lower.State, "open_bit", !open) is not { } updatedLower ||
            BlockPalette.Find(top.Identifier, top.State, "open_bit", !open) is not { } updatedTop)
            return true;

        dimension.SetPermutation(position.X, lowerY, position.Z, updatedLower);
        dimension.SetPermutation(position.X, upperY, position.Z, updatedTop);

        dimension.Broadcast(new UpdateBlockPacket {
            Position = new BlockPos { X = position.X, Y = lowerY, Z = position.Z },
            BlockRuntimeId = unchecked((uint)updatedLower.Hash),
            Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network
        }, position.X >> 4, position.Z >> 4, additionalPacket: new LevelEventPacket {
            EventId = 1003,
            Position = new Vec3 { X = position.X + 0.5f, Y = lowerY + 0.5f, Z = position.Z + 0.5f }
        });
        dimension.Broadcast(new UpdateBlockPacket {
            Position = new BlockPos { X = position.X, Y = upperY, Z = position.Z },
            BlockRuntimeId = unchecked((uint)updatedTop.Hash),
            Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network
        }, position.X >> 4, position.Z >> 4);
        return true;
    }

    protected override bool? HasSupport(Dimension dimension, int x, int y, int z) {
        BlockPermutation? current = dimension.GetLoadedPermutation(x, y, z);
        if (current is null) return null;
        if (!BlockPalette.IsDoor(current.Identifier) ||
            current.State.Values.GetValueOrDefault("upper_block_bit") is not bool upper)
            return false;

        if (upper) {
            BlockPermutation? lower = dimension.GetLoadedPermutation(x, y - 1, z);
            if (lower is null) return null;
            return lower.Identifier == current.Identifier && lower.State.Values.GetValueOrDefault("upper_block_bit") is false;
        }

        BlockPermutation? top = dimension.GetLoadedPermutation(x, y + 1, z);
        BlockPermutation? support = dimension.GetLoadedPermutation(x, y - 1, z);
        if (top is null || support is null) return null;

        bool validTop = top.Identifier == current.Identifier && top.State.Values.GetValueOrDefault("upper_block_bit") is true;
        bool supported = BlockPalette.FindType(support.Identifier)?.Solid == true;
        return validTop && supported;
    }

    protected override void OnSupportLost(Dimension dimension, int x, int y, int z, bool dropItem) {
        BlockPermutation current = dimension.GetPermutation(x, y, z);
        bool upper = current.State.Values.GetValueOrDefault("upper_block_bit") is true;
        if (!upper) {
            BlockPermutation top = dimension.GetPermutation(x, y + 1, z);
            if (top.Identifier == current.Identifier && top.State.Values.GetValueOrDefault("upper_block_bit") is true) {
                dimension.SetPermutation(x, y + 1, z, BlockStorage.Air);
                dimension.Broadcast(new UpdateBlockPacket {
                    Position = new BlockPos { X = x, Y = y + 1, Z = z },
                    BlockRuntimeId = unchecked((uint)BlockStorage.Air.Hash),
                    Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network
                }, x >> 4, z >> 4);
                Vec3 upperPosition = new() { X = x + 0.5f, Y = y + 1.5f, Z = z + 0.5f };
                dimension.Broadcast(new LevelEventPacket {
                    EventId = (int)LevelEvent.ParticlesDestroyBlock,
                    Position = upperPosition,
                    Data = top.Hash
                }, upperPosition);
                dimension.Broadcast(new LevelSoundEventPacket {
                    SoundEvent = "break",
                    Position = upperPosition,
                    Data = top.NetworkId,
                    ActorUniqueId = -1
                }, upperPosition);
            }
        }

        base.OnSupportLost(dimension, x, y, z, dropItem && !upper);
    }
}
