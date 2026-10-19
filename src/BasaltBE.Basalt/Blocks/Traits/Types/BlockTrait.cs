using BasaltBE.Basalt.Entities;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits.Types;

public abstract class BlockTrait {
    public virtual bool OnInteract(Player player, BlockPos position) => false;
}
