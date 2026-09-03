namespace Basalt.Core.Item.Components;

using Basalt.BedrockProtocol.NBT;


/// <summary>
/// Represents the "minecraft:hover_text_color" component that defines
/// the color used for the item's hover text.
/// </summary>
public sealed class ItemTypeHoverTextColorComponent : ItemTypeComponent {
    public new static string Identifier => "minecraft:hover_text_color";

    public ItemTypeHoverTextColorComponent(ItemType type, CompoundTag component) : base(type, component) {
    }

    /// <summary>
    /// Gets the hover text color name (e.g. "minecoin_gold", "red").
    /// </summary>
    public string GetColor() {
        return Component.Get<StringTag>("value")?.Value ?? string.Empty;
    }
}
