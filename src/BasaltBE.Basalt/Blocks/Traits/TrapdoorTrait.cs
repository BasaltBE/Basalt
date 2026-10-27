using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

public sealed class TrapdoorTrait : BlockTrait {
    public override bool OnPlace(Player player, int x, int y, int z, byte face, Vec3 clickPosition, ref BlockPermutation permutation) {
        Dimension dimension = player.Dimension;
        bool supported = BlockPalette.FindType(dimension.GetPermutation(x + 1, y, z).Identifier)?.Solid == true ||
            BlockPalette.FindType(dimension.GetPermutation(x - 1, y, z).Identifier)?.Solid == true ||
            BlockPalette.FindType(dimension.GetPermutation(x, y, z + 1).Identifier)?.Solid == true ||
            BlockPalette.FindType(dimension.GetPermutation(x, y, z - 1).Identifier)?.Solid == true ||
            BlockPalette.FindType(dimension.GetPermutation(x, y + 1, z).Identifier)?.Solid == true ||
            BlockPalette.FindType(dimension.GetPermutation(x, y - 1, z).Identifier)?.Solid == true;
        if (!supported)
            return false;

        int facing = (int)MathF.Floor((player.Rotation.Y + 45) / 90) & 3;
        int direction = BlockDirections.TrapdoorDirectionByYaw[facing];
        bool upsideDown = face == 0 || face is not (0 or 1) && clickPosition.Y > 0.5f;
        permutation = BlockPalette.Find(permutation.Identifier, permutation.State, "direction", direction) ?? permutation;
        permutation = BlockPalette.Find(permutation.Identifier, permutation.State, "upside_down_bit", upsideDown) ?? permutation;
        permutation = BlockPalette.Find(permutation.Identifier, permutation.State, "open_bit", false) ?? permutation;
        return true;
    }

    public override bool OnInteract(Player player, BlockPos position) {
        BlockPermutation current = player.Dimension.GetPermutation(position.X, position.Y, position.Z);
        if (!BlockPalette.IsTrapdoor(current.Identifier))
            return false;
        if (current.Identifier == "minecraft:iron_trapdoor")
            return true;
        if (current.State.Values.GetValueOrDefault("open_bit") is not bool open ||
            BlockPalette.Find(current.Identifier, current.State, "open_bit", !open) is not { } updated)
            return true;

        player.Dimension.SetPermutation(position.X, position.Y, position.Z, updated);
        player.Dimension.Broadcast(new UpdateBlockPacket {
            Position = position,
            BlockRuntimeId = unchecked((uint)updated.Hash),
            Flags = UpdateBlockFlags.Neighbors | UpdateBlockFlags.Network
        }, position.X >> 4, position.Z >> 4, additionalPacket: new LevelSoundEventPacket {
            SoundEvent = open ? "trapdoor.close" : "trapdoor.open",
            Position = new Vec3 { X = position.X + 0.5f, Y = position.Y + 0.5f, Z = position.Z + 0.5f },
            ActorUniqueId = -1
        });
        return true;
    }

    public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) {
        BlockPermutation current = dimension.GetPermutation(x, y, z);
        if (!BlockPalette.IsTrapdoor(current.Identifier))
            return false;

        if (BlockPalette.FindType(dimension.GetPermutation(x + 1, y, z).Identifier)?.Solid == true ||
            BlockPalette.FindType(dimension.GetPermutation(x - 1, y, z).Identifier)?.Solid == true ||
            BlockPalette.FindType(dimension.GetPermutation(x, y, z + 1).Identifier)?.Solid == true ||
            BlockPalette.FindType(dimension.GetPermutation(x, y, z - 1).Identifier)?.Solid == true ||
            BlockPalette.FindType(dimension.GetPermutation(x, y + 1, z).Identifier)?.Solid == true ||
            BlockPalette.FindType(dimension.GetPermutation(x, y - 1, z).Identifier)?.Solid == true)
            return false;

        dimension.SetPermutation(x, y, z, BlockStorage.Air);
        return true;
    }
}
