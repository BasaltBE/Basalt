namespace Basalt.Core.Item.Components;

using Basalt.Protocol.Nbt;


/// <summary>
/// Represents the "minecraft:icon" component that defines the item's texture.
/// </summary>
public sealed class ItemTypeIconComponent : ItemTypeComponent
{
  public new static string Identifier => "minecraft:icon";

  public ItemTypeIconComponent(ItemType type, CompoundTag component) : base(type, component)
  {
  }

  /// <summary>
  /// Gets the texture name used for rendering the item.
  /// </summary>
  public string GetTexture()
  {
    return Component.Get<StringTag>("texture")?.Value
           ?? Component.Get<StringTag>("textures")?.Value
           ?? string.Empty;
  }

  /// <summary>
  /// Gets the legacy identifier for backwards compatibility.
  /// </summary>
  public int GetLegacyId()
  {
    return Component.Get<IntTag>("legacy_id")?.Value ?? 0;
  }

  /// <summary>
  /// Gets the frame index for animated textures.
  /// </summary>
  public int GetFrame()
  {
    return Component.Get<IntTag>("frame")?.Value ?? 0;
  }
}
