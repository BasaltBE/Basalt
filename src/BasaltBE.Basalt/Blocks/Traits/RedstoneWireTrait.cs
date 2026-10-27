using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo("minecraft:redstone_wire")]
public sealed class RedstoneWireTrait : BlockSupportTrait {
    public override bool OnPlace(Player player, int x, int y, int z, byte face, Vec3 clickPosition, ref BlockPermutation permutation) =>
        HasSupport(player.Dimension, x, y, z) is true;

    public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) {
        dimension.Redstone.MarkDirty(x, y, z);
        return base.OnNeighborUpdate(dimension, x, y, z);
    }

    protected override bool? HasSupport(Dimension dimension, int x, int y, int z) {
        BlockPermutation? support = dimension.GetLoadedPermutation(x, y - 1, z);
        return support is null ? null : BlockPalette.FindType(support.Identifier)?.Solid == true;
    }
}
