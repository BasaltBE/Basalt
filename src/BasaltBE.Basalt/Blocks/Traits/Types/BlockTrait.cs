using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits.Types;

public abstract class BlockTrait {
    public virtual bool OnInteract(Player player, BlockPos position) => false;

    public virtual bool OnPlace(
        Player player,
        int x,
        int y,
        int z,
        byte face,
        Vec3 clickPosition,
        ref BlockPermutation permutation
    ) => true;

    public virtual void OnPlaced(Dimension dimension, int x, int y, int z) { }

    public virtual bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) => false;

    public virtual void OnScheduledUpdate(Dimension dimension, int x, int y, int z) { }

    internal virtual bool HasScheduledUpdates => false;

    internal virtual void OnScheduledUpdate(Dimension dimension, int x, int y, int z, BlockPermutation current) =>
        OnScheduledUpdate(dimension, x, y, z);

    internal virtual void OnScheduledUpdate(
        Dimension dimension,
        int x,
        int y,
        int z,
        BlockPermutation current,
        BlockPermutation scheduled
    ) {
        if (current.Identifier == scheduled.Identifier)
            OnScheduledUpdate(dimension, x, y, z, current);
    }
}
