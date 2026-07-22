namespace Basalt.Core.Item.Enchantment;

using Basalt.Core.Entities;
using Basalt.Core.Player;
using Basalt.Protocol.Types;

/// <summary>
/// Context passed to enchantments when a block is broken.
/// Enchantments can modify drops or cancel durability damage.
/// </summary>
public sealed class BlockBreakEnchantmentContext {
    public required Player Player { get; init; }
    public required BlockPos Position { get; init; }
    public required string BlockIdentifier { get; init; }
    public List<ItemStack> Drops { get; set; } = [];
    public bool PreventDurabilityLoss { get; set; }
    public bool SilkTouch { get; set; }
}

/// <summary>
/// Context passed to enchantments when an entity is attacked.
/// Enchantments can modify damage, knockback, or apply effects.
/// </summary>
public sealed class AttackEntityEnchantmentContext {
    public required Player Player { get; init; }
    public required Entity Target { get; init; }
    public float BonusDamage { get; set; }
    public float KnockbackMultiplier { get; set; } = 1f;
    public int FireTicks { get; set; }
    public int LootingLevel { get; set; }
}

/// <summary>
/// Context passed to enchantments when the wearer takes damage.
/// Enchantments can reduce damage or reflect it.
/// </summary>
public sealed class HurtEnchantmentContext {
    public required Player Player { get; init; }
    public Entity? Attacker { get; init; }
    public required float Damage { get; init; }
    public required DamageSource Source { get; init; }
    public float DamageReduction { get; set; }
    public float ReflectedDamage { get; set; }
}

/// <summary>
/// Context passed to enchantments every tick while equipped.
/// Used for passive effects like Frost Walker or Soul Speed.
/// </summary>
public sealed class TickEnchantmentContext {
    public required Player Player { get; init; }
    public required EquipmentSlot Slot { get; init; }
}

public enum DamageSource {
    Generic,
    Melee,
    Projectile,
    Fire,
    Explosion,
    Fall,
    Magic,
    Void
}

public enum EquipmentSlot {
    Mainhand,
    Offhand,
    Head,
    Chest,
    Legs,
    Feet
}
