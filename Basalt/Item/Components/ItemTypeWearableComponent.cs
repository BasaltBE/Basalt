namespace Basalt.Core.Item.Components;

using Basalt.Protocol.Nbt;

public sealed class ItemTypeWearableComponent : ItemTypeComponent {
    public new static string Identifier => "minecraft:wearable";

    public ItemTypeWearableComponent(ItemType type, CompoundTag component) : base(type, component) {
    }

    public int GetSlot() {
        if (Component.Get<IntTag>("slot") is IntTag intSlot) {
            return intSlot.Value;
        }

        if (Component.Get<StringTag>("slot") is StringTag strSlot) {
            return strSlot.Value switch {
                "slot.armor.head" => 0,
                "slot.armor.chest" => 1,
                "slot.armor.legs" => 2,
                "slot.armor.feet" => 3,
                _ => -1
            };
        }

        return -1;
    }
}
