namespace Basalt.Core.Item.Enchantment;

using Basalt.Core.Item.Enchantment.Types;

/// <summary>
/// Registy all vanilla enchantments.
/// </summary>
internal static class EnchantmentRegistry
{
  private static bool _registered;

  public static void RegisterVanilla()
  {
    if (_registered) return;
    _registered = true;

    // Protection
    EnchantmentType.Register(new ProtectionEnchantment());
    EnchantmentType.Register(new FireProtectionEnchantment());
    EnchantmentType.Register(new FeatherFallingEnchantment());
    EnchantmentType.Register(new BlastProtectionEnchantment());
    EnchantmentType.Register(new ProjectileProtectionEnchantment());
    EnchantmentType.Register(new ThornsEnchantment());
    EnchantmentType.Register(new RespirationEnchantment());
    EnchantmentType.Register(new DepthStriderEnchantment());
    EnchantmentType.Register(new AquaAffinityEnchantment());

    // Melee
    EnchantmentType.Register(new SharpnessEnchantment());
    EnchantmentType.Register(new SmiteEnchantment());
    EnchantmentType.Register(new BaneOfArthropodsEnchantment());
    EnchantmentType.Register(new KnockbackEnchantment());
    EnchantmentType.Register(new FireAspectEnchantment());
    EnchantmentType.Register(new LootingEnchantment());

    // Tools
    EnchantmentType.Register(new EfficiencyEnchantment());
    EnchantmentType.Register(new SilkTouchEnchantment());
    EnchantmentType.Register(new UnbreakingEnchantment());
    EnchantmentType.Register(new FortuneEnchantment());

    // Bow
    EnchantmentType.Register(new PowerEnchantment());
    EnchantmentType.Register(new PunchEnchantment());
    EnchantmentType.Register(new FlameEnchantment());
    EnchantmentType.Register(new InfinityEnchantment());

    // Fishing
    EnchantmentType.Register(new LuckOfTheSeaEnchantment());
    EnchantmentType.Register(new LureEnchantment());

    // Misc
    EnchantmentType.Register(new FrostWalkerEnchantment());
    EnchantmentType.Register(new MendingEnchantment());
    EnchantmentType.Register(new CurseOfBindingEnchantment());
    EnchantmentType.Register(new CurseOfVanishingEnchantment());

    // Trident
    EnchantmentType.Register(new ImpalingEnchantment());
    EnchantmentType.Register(new RiptideEnchantment());
    EnchantmentType.Register(new LoyaltyEnchantment());
    EnchantmentType.Register(new ChannelingEnchantment());

    // Crossbow
    EnchantmentType.Register(new MultishotEnchantment());
    EnchantmentType.Register(new PiercingEnchantment());
    EnchantmentType.Register(new QuickChargeEnchantment());

    // Soul/Swift
    EnchantmentType.Register(new SoulSpeedEnchantment());
    EnchantmentType.Register(new SwiftSneakEnchantment());

    // Mace 
    EnchantmentType.Register(new WindBurstEnchantment());
    EnchantmentType.Register(new DensityEnchantment());
    EnchantmentType.Register(new BreachEnchantment());
    EnchantmentType.Register(new LungeEnchantment());
  }
}
