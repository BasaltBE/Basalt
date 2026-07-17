namespace Basalt.Core.Item.Traits;

using Basalt.Core.Entities.Traits;
using Basalt.Core.Item.Components;
using Basalt.Core.Item.Traits.Types;

public sealed class ItemStackWearableTrait : ItemTrait
{
    public new static string Identifier => "wearable";
    public new static readonly Type? Component = typeof(ItemTypeWearableComponent);

    // Wearable slot enum from the protocol.
    // 0 = head, 1 = chest, 2 = legs, 3 = feet, 4 = offhand
    private const int SlotHead = 0;
    private const int SlotChest = 1;
    private const int SlotLegs = 2;
    private const int SlotFeet = 3;

    public int Slot { get; set; } = -1;

    public ItemStackWearableTrait(ItemStack itemStack) : base(itemStack)
    {
    }

    public override void OnAdd()
    {
        ItemTypeWearableComponent? component = ItemStack.Type.Components.GetComponent<ItemTypeWearableComponent>();
        if (component is not null)
        {
            Slot = component.GetSlot();
        }
    }

    public override void OnUseOnAir(ItemUseOnAirDetails details)
    {
        TryEquip(details.Player, details.HotBarSlot);
    }

    private void TryEquip(Player.Player player, int hotbarSlot)
    {
        if (Slot < SlotHead || Slot > SlotFeet)
        {
            return;
        }

        EntityEquipmentTrait? equipment = player.GetTrait<EntityEquipmentTrait>();
        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (equipment is null || inventory is null)
        {
            return;
        }

        ItemStack? existing = equipment.Armor.GetItem(Slot);
        inventory.Container.ClearSlot(hotbarSlot);

        if (existing is not null)
        {
            inventory.Container.SetItem(hotbarSlot, existing);
        }

        equipment.Armor.SetItem(Slot, ItemStack);
    }
}
