using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo("minecraft:grindstone")]
public sealed class GrindstoneTrait : BlockTrait {
    public override bool OnInteract(Player player, BlockPos position) {
        GrindstoneContainer container = new(player, position);
        container.Changed += container.Refresh;
        container.LastViewerClosed += container.ReturnItems;
        return container.Open(player);
    }
}
