namespace Basalt.Core.Item.Components;

using BedrockProtocol.Nbt;


/// <summary>
/// Represents the "minecraft:foil" component that controls
/// whether the item has the enchanted glint render effect.
/// </summary>
public sealed class ItemTypeGlintComponent : ItemTypeComponent {
    public new static string Identifier => "minecraft:foil";

    public ItemTypeGlintComponent(ItemType type, CompoundTag component) : base(type, component) {
    }

    /// <summary>
    /// Whether the item displays the enchanted glint effect.
    /// </summary>
    public bool HasGlint() {
        return (Component.Get<ByteTag>("value")?.Value ?? 0) != 0;
    }
}
