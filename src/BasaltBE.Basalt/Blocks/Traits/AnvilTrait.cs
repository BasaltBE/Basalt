using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo("minecraft:anvil", "minecraft:chipped_anvil", "minecraft:damaged_anvil")]
public sealed class AnvilTrait : BlockTrait {
    public override bool OnInteract(Player player, BlockPos position) {
        AnvilContainer container = new(player, position);
        container.Changed += container.Refresh;
        container.LastViewerClosed += container.ReturnItems;
        if (!container.Open(player))
            return false;
        container.Refresh();
        return true;
    }

    public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) {
        if (y <= dimension.MinimumY || BlockPalette.FindType(dimension.GetPermutation(x, y - 1, z).Identifier)?.Solid == true)
            return false;

        BlockPermutation anvil = dimension.GetPermutation(x, y, z);
        dimension.SetPermutation(x, y, z, BlockStorage.Air);
        new FallingBlockEntity(anvil, x + 0.5f, y, z + 0.5f).Spawn(dimension);
        return true;
    }
}
