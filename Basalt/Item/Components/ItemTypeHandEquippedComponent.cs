namespace Basalt.Core.Item.Components;

using Basalt.Protocol.Nbt;


/// <summary>
/// Represents the "minecraft:hand_equipped" component that controls
/// whether the item renders like a tool when held.
/// </summary>
public sealed class ItemTypeHandEquippedComponent : ItemTypeComponent
{
  public new static string Identifier => "minecraft:hand_equipped";

  public ItemTypeHandEquippedComponent(ItemType type, CompoundTag component) : base(type, component)
  {
  }

  /// <summary>
  /// Whether the item uses the tool-style rendering when held.
  /// </summary>
  public bool IsHandEquipped()
  {
    return (Component.Get<ByteTag>("value")?.Value ?? 0) != 0;
  }
}
