using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo("minecraft:crafting_table")]
public sealed class CraftingTableTrait : BlockTrait {
    public override bool OnInteract(Player player, BlockPos position) {
        if (player.GetTrait<PlayerInventoryTrait>() is not { } inventory) return false;

        BlockPos tablePosition = new() { X = position.X, Y = position.Y, Z = position.Z };
        BlockContainer table = new(
            tablePosition,
            9,
            ContainerId.First,
            ContainerEnumName.CraftingInput,
            ContainerType.Workbench,
            slotOffset: 32
        );
        table.LastViewerClosed += () => {
            for (int slot = 0; slot < table.ContainerSize; slot++) {
                if (table.TakeItem(slot, ushort.MaxValue) is not { } ingredient) continue;
                if (inventory.Inventory.AddItem(ingredient) is { } remaining)
                    new ItemEntity(remaining, player, tablePosition.X, tablePosition.Y, tablePosition.Z).Spawn(player.Dimension);
            }
        };
        table.Open(player);
        return true;
    }
}
