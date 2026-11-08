using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo("minecraft:lever")]
public sealed class LeverTrait : BlockSupportTrait {
    static readonly Dictionary<string, (int X, int Y, int Z)> SupportOffsets = new(StringComparer.Ordinal) {
        ["down_east_west"] = (0, 1, 0),
        ["down_north_south"] = (0, 1, 0),
        ["up_east_west"] = (0, -1, 0),
        ["up_north_south"] = (0, -1, 0),
        ["east"] = (-1, 0, 0),
        ["west"] = (1, 0, 0),
        ["north"] = (0, 0, 1),
        ["south"] = (0, 0, -1)
    };

    public override bool OnPlace(Player player, int x, int y, int z, byte face, Vec3 clickPosition, ref BlockPermutation permutation) {
        if (face > 5) return false;

        int facing = (int)MathF.Floor((player.Rotation.Y + 45) / 90) & 3;
        string direction = face < 2
            ? $"{BlockDirections.ByFace[face]}_{BlockDirections.FloorAxisByYaw[facing]}"
            : BlockDirections.ByFace[face];
        if (BlockPalette.Find(permutation.Identifier, permutation.State, "lever_direction", direction) is not { } faced ||
            BlockPalette.Find(faced.Identifier, faced.State, "open_bit", false) is not { } closed)
            return false;

        permutation = closed;
        (int supportX, int supportY, int supportZ) = SupportOffsets[direction];
        BlockPermutation? support = player.Dimension.GetLoadedPermutation(x + supportX, y + supportY, z + supportZ);
        return support is not null && BlockPalette.FindType(support.Identifier)?.Solid == true;
    }

    public override bool OnInteract(Player player, BlockPos position) {
        Dimension dimension = player.Dimension;
        BlockPermutation current = dimension.GetPermutation(position.X, position.Y, position.Z);
        if (current.Identifier != "minecraft:lever" ||
            current.State.Values.GetValueOrDefault("open_bit") is not bool open ||
            BlockPalette.Find(current.Identifier, current.State, "open_bit", !open) is not { } updated)
            return current.Identifier == "minecraft:lever";

        dimension.SetPermutation(position.X, position.Y, position.Z, updated);
        dimension.BroadcastBlockUpdate(position.X, position.Y, position.Z, updated);
        dimension.Broadcast(new LevelSoundEventPacket {
            SoundEvent = "random.click",
            Position = new Vec3 { X = position.X + 0.5f, Y = position.Y + 0.5f, Z = position.Z + 0.5f },
            ActorUniqueId = -1
        }, position.X >> 4, position.Z >> 4);
        dimension.UpdateNeighbors(position.X, position.Y, position.Z);
        return true;
    }

    protected override bool? HasSupport(Dimension dimension, int x, int y, int z) {
        BlockPermutation? current = dimension.GetLoadedPermutation(x, y, z);
        if (current is null) return null;
        if (current.State.Values.GetValueOrDefault("lever_direction") is not string direction ||
            !SupportOffsets.TryGetValue(direction, out var offset))
            return false;

        BlockPermutation? support = dimension.GetLoadedPermutation(x + offset.X, y + offset.Y, z + offset.Z);
        return support is null ? null : BlockPalette.FindType(support.Identifier)?.Solid == true;
    }
}
