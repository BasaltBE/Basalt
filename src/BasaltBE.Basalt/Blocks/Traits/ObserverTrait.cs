using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo("minecraft:observer")]
public sealed class ObserverTrait : BlockTrait {
    internal override bool HasScheduledUpdates => true;

    public override bool OnPlace(Player player, int x, int y, int z, byte face, Vec3 clickPosition, ref BlockPermutation permutation) {
        int facing = (int)MathF.Floor((player.Rotation.Y + 45) / 90) & 3;
        if (BlockPalette.Find(permutation.Identifier, permutation.State, "minecraft:facing_direction", BlockDirections.ByYaw[facing]) is not { } faced)
            return false;

        permutation = faced;
        return true;
    }

    internal override void OnScheduledUpdate(Dimension dimension, int x, int y, int z, BlockPermutation current) {
        bool powered = current.State.Values.GetValueOrDefault("powered_bit") is true;
        if (BlockPalette.Find(current.Identifier, current.State, "powered_bit", !powered) is not { } updated)
            return;

        dimension.SetPermutation(x, y, z, updated);
        dimension.BroadcastBlockUpdate(x, y, z, updated);
        if (!powered)
            dimension.ScheduleBlockUpdate(x, y, z, 2);
    }
}
