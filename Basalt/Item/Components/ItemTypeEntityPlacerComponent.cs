namespace Basalt.Core.Item.Components;

using Basalt.Protocol.Nbt;

public sealed class ItemTypeEntityPlacerComponent : ItemTypeComponent {
    public new static string Identifier => "minecraft:entity_placer";

    public ItemTypeEntityPlacerComponent(ItemType type, CompoundTag component) : base(type, component) {
    }

    public string GetEntity() {
        return Component.Get<StringTag>("entity")?.Value ?? string.Empty;
    }
}
