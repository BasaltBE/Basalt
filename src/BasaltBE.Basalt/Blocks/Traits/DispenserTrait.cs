using BasaltBE.Basalt.Blocks.BlockActors;
using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo("minecraft:dispenser", "minecraft:dropper")]
public sealed class DispenserTrait : BlockTrait {
    internal override bool HasScheduledUpdates => true;

    public override bool OnPlace(Player player, int x, int y, int z, byte face, Vec3 clickPosition, ref BlockPermutation permutation) {
        int facing = BlockDirections.FacingByRotation(player.Rotation.X, player.Rotation.Y);
        permutation = BlockPalette.Find(permutation.Identifier, permutation.State, "facing_direction", facing) ?? permutation;
        return permutation.State.Values.GetValueOrDefault("facing_direction") is int placedFacing && placedFacing == facing;
    }

    public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) {
        bool? powered = dimension.Redstone.BlockPowered(dimension, x, y, z);
        if (powered is not bool isPowered) return false;

        BlockPermutation current = dimension.GetPermutation(x, y, z);
        if (current.State.Values.GetValueOrDefault("triggered_bit") is not bool triggered || triggered == isPowered ||
            BlockPalette.Find(current.Identifier, current.State, "triggered_bit", isPowered) is not { } updated)
            return false;

        dimension.SetPermutation(x, y, z, updated);
        dimension.BroadcastBlockUpdate(x, y, z, updated);
        if (isPowered) dimension.ScheduleBlockUpdate(x, y, z, 4);
        return true;
    }

    internal override void OnScheduledUpdate(Dimension dimension, int x, int y, int z, BlockPermutation current) {
        if (dimension.GetChunk(x >> 4, z >> 4) is { } chunk &&
            dimension.GetBlockActor(chunk, x & 15, y, z & 15) is DispenserBlockActor dispenser)
            dispenser.Activate();
    }
}
