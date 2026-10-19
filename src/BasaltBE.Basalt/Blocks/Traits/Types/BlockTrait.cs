using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits.Types;

public abstract class BlockTrait {
    public virtual bool OnInteract(Player player, BlockPos position) => false;

    public virtual bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) => false;
}
