using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Worlds.Dimensions;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo(
    "minecraft:pale_oak_fence",
    "minecraft:jungle_fence",
    "minecraft:spruce_fence",
    "minecraft:bamboo_fence",
    "minecraft:cherry_fence",
    "minecraft:nether_brick_fence",
    "minecraft:oak_fence",
    "minecraft:warped_fence",
    "minecraft:mangrove_fence",
    "minecraft:dark_oak_fence",
    "minecraft:poplar_fence",
    "minecraft:crimson_fence"
)]
public sealed class FenceTrait : BlockTrait {
    public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) {
        BlockPermutation current = dimension.GetPermutation(x, y, z);
        byte connections = 0;
        for (int index = 0; index < BlockDirections.Connections.Length; index++) {
            var direction = BlockDirections.Connections[index];
            BlockPermutation neighbor = dimension.GetPermutation(x + direction.X, y, z + direction.Z);
            if (BlockPalette.FenceConnects(current.Identifier, neighbor.Identifier)) connections |= (byte)(1 << index);
        }

        BlockPermutation? updated = BlockPalette.FindFencePermutation(current.Identifier, connections);
        if (updated is null || updated == current) return false;

        dimension.SetPermutation(x, y, z, updated);
        return true;
    }
}
