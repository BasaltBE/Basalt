namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Container;
using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Containers;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Item;
using Basalt.Protocol.Types;

public sealed class CraftingTableTrait : BlockTrait {
    public override bool Interactable => true;
    public static new readonly string Identifier = "crafting_table";
    public static new readonly string[] Types = ["minecraft:crafting_table"];

    private BlockContainer? _container;

    public CraftingTableTrait(Block block) : base(block) {
    }

    public override void OnInteract(BlockInteractDetails details) {
        var dimension = details.Player.Dimension;
        if (dimension is null) return;

        if (_container is null) {
            _container = new BlockContainer(
              dimension,
              details.BlockPosition,
              ContainerType.Workbench,
              9);

            _container.OnViewerRemovedEvent = OnViewerRemoved;
        }

        _container.Show(details.Player);
    }

    public override void OnBreak(BlockBreakDetails details) {
        if (_container is null) return;

        foreach ((Player.Player player, _) in _container.GetAllOccupants().ToList()) {
            _container.Close(player);
        }

        _container = null;
    }

    private static void OnViewerRemoved(BlockContainer container, Player.Player player) {
        for (int i = 0; i < container.GetSize(); i++) {
            ItemStack? item = container.GetItem(i);
            if (item is null || item.StackSize == 0) continue;

            container.ClearSlot(i);

            var inventory = player.GetTrait<EntityInventoryTrait>();
            if (inventory is null || !inventory.Container.AddItem(item)) {
                player.DropItem(item);
            }
        }
    }
}
