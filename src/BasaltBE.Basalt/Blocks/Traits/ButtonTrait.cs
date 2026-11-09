using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

public sealed class ButtonTrait : BlockSupportTrait {
    internal override bool HasScheduledUpdates => true;

    static readonly (int X, int Y, int Z)[] SupportOffsets = [
        (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1), (1, 0, 0), (-1, 0, 0)
    ];

    public override bool OnPlace(Player player, int x, int y, int z, byte face, Vec3 clickPosition, ref BlockPermutation permutation) {
        if (face > 5) return false;

        if (BlockPalette.Find(permutation.Identifier, permutation.State, "facing_direction", (int)face) is not { } faced ||
            BlockPalette.Find(faced.Identifier, faced.State, "button_pressed_bit", false) is not { } released)
            return false;

        permutation = released;
        (int supportX, int supportY, int supportZ) = SupportOffsets[face];
        BlockPermutation? support = player.Dimension.GetLoadedPermutation(x + supportX, y + supportY, z + supportZ);
        return support is not null && BlockPalette.FindType(support.Identifier)?.Solid == true;
    }

    public override bool OnInteract(Player player, BlockPos position) {
        Dimension dimension = player.Dimension;
        BlockPermutation current = dimension.GetPermutation(position.X, position.Y, position.Z);
        if (!current.State.Values.ContainsKey("button_pressed_bit")) return false;
        if (current.State.Values.GetValueOrDefault("button_pressed_bit") is true) return true;
        if (BlockPalette.Find(current.Identifier, current.State, "button_pressed_bit", true) is not { } pressed) return true;

        dimension.SetPermutation(position.X, position.Y, position.Z, pressed);
        dimension.BroadcastBlockUpdate(position.X, position.Y, position.Z, pressed);
        dimension.ScheduleBlockUpdate(position.X, position.Y, position.Z, 30);
        dimension.Broadcast(new LevelSoundEventPacket {
            SoundEvent = "random.click",
            Position = new Vec3 { X = position.X + 0.5f, Y = position.Y + 0.5f, Z = position.Z + 0.5f },
            ActorUniqueId = -1
        }, position.X >> 4, position.Z >> 4);
        dimension.UpdateNeighbors(position.X, position.Y, position.Z);
        return true;
    }

    internal override void OnScheduledUpdate(Dimension dimension, int x, int y, int z, BlockPermutation current) {
        if (!current.State.Values.ContainsKey("button_pressed_bit") ||
            current.State.Values.GetValueOrDefault("button_pressed_bit") is not true ||
            BlockPalette.Find(current.Identifier, current.State, "button_pressed_bit", false) is not { } released)
            return;

        dimension.SetPermutation(x, y, z, released);
        dimension.BroadcastBlockUpdate(x, y, z, released);
        dimension.Broadcast(new LevelSoundEventPacket {
            SoundEvent = "random.click",
            Position = new Vec3 { X = x + 0.5f, Y = y + 0.5f, Z = z + 0.5f },
            ActorUniqueId = -1
        }, x >> 4, z >> 4);
        dimension.UpdateNeighbors(x, y, z);
    }

    protected override bool? HasSupport(Dimension dimension, int x, int y, int z) {
        BlockPermutation? current = dimension.GetLoadedPermutation(x, y, z);
        if (current is null) return null;
        if (current.State.Values.GetValueOrDefault("facing_direction") is not int facing || (uint)facing >= SupportOffsets.Length)
            return false;

        (int offsetX, int offsetY, int offsetZ) = SupportOffsets[facing];
        BlockPermutation? support = dimension.GetLoadedPermutation(x + offsetX, y + offsetY, z + offsetZ);
        return support is null ? null : BlockPalette.FindType(support.Identifier)?.Solid == true;
    }
}
