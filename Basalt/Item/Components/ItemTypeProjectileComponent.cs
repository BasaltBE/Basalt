namespace Basalt.Core.Item.Components;

using BedrockProtocol.Nbt;


/// <summary>
/// Represents the "minecraft:projectile" component that defines
/// projectile behavior for items like bows.
/// </summary>
public sealed class ItemTypeProjectileComponent : ItemTypeComponent {
    public new static string Identifier => "minecraft:projectile";

    public ItemTypeProjectileComponent(ItemType type, CompoundTag component) : base(type, component) {
    }

    /// <summary>
    /// Gets the minimum power required for a critical hit.
    /// </summary>
    public float GetMinimumCriticalPower() {
        return Component.Get<FloatTag>("minimum_critical_power")?.Value
               ?? Component.Get<FloatTag>("minimumCriticalPower")?.Value
               ?? 0f;
    }

    /// <summary>
    /// Gets the entity identifier for the projectile spawned.
    /// </summary>
    public string GetProjectileEntity() {
        return Component.Get<StringTag>("projectile_entity")?.Value
               ?? Component.Get<StringTag>("projectileEntity")?.Value
               ?? string.Empty;
    }
}
