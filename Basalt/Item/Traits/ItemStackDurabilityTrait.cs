namespace Basalt.Core.Item.Traits;

using Basalt.Core.Entities.Traits;
using Basalt.Core.Item.Components;
using Basalt.Core.Item.Traits.Types;
using Basalt.Core.Player;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

public sealed class ItemStackDurabilityTrait : ItemTrait {
  public new static string Identifier => "durability";
  public new static readonly Type? Component = typeof(ItemTypeDurabilityComponent);

  private int _maxDurability;
  private int _damageChanceMin;
  private int _damageChanceMax;
  private int _damage;

  public ItemStackDurabilityTrait(ItemStack itemStack) : base(itemStack) {
  }

  public override void OnAdd() {
    ItemTypeDurabilityComponent? durability =
      ItemStack.Type.Components.GetComponent<ItemTypeDurabilityComponent>();

    if (durability is null) {
      return;
    }

    _maxDurability = durability.GetMaxDurability();
    (int min, int max) = durability.GetDamageChance();
    _damageChanceMin = min;
    _damageChanceMax = max;

    // Initialize from metadata (vanilla world format stores damage there).
    _damage = unchecked((int)ItemStack.Metadata);
    if (_damage > 0) {
      SyncDamageTag();
    }
  }

  public int GetCurrentDamage() {
    return _damage;
  }

  public int GetMaxDurability() {
    return _maxDurability;
  }

  public int GetRemainingDurability() {
    return _maxDurability - _damage;
  }

  public void Repair(int amount) {
    if (amount <= 0 || _damage <= 0) {
      return;
    }

    _damage = Math.Max(0, _damage - amount);
    SyncDamageTag();
  }

  public override void OnRead(CompoundTag tag) {
    _damage = tag.Get<IntTag>("Damage")?.Value ?? 0;
    if (_damage > 0) {
      SyncDamageTag();
    }
  }

  public override void OnWrite(CompoundTag tag) {
    tag.Set("Damage", new IntTag { Value = _damage });
  }

  public override void OnBreakBlock(ItemBreakBlockDetails details) {
    ApplyDamage(details.Player, details.HotBarSlot);
  }

  public override void OnUseAttack(ItemUseAttackDetails details) {
    ApplyDamage(details.Player, details.HotBarSlot);
  }

  /// <summary>
  /// Applies durability damage to armor worn by the entity.
  /// </summary>
  public void ApplyArmorDamage(Basalt.Core.Containers.Container armorContainer, int slot) {
    if (_maxDurability <= 0) {
      return;
    }

    if (HasUnbreakingProtection()) {
      return;
    }

    if (!PassesDamageChance()) {
      return;
    }

    _damage++;
    SyncDamageTag();

    if (_damage >= _maxDurability) {
      armorContainer.ClearSlot(slot);
      return;
    }

    armorContainer.UpdateSlot(slot);
  }

  private void ApplyDamage(Player player, int slot) {
    if (_maxDurability <= 0) {
      return;
    }

    if (HasUnbreakingProtection()) {
      return;
    }

    if (!PassesDamageChance()) {
      return;
    }

    _damage++;
    SyncDamageTag();

    EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();

    if (_damage >= _maxDurability) {
      inventory?.Container.ClearSlot(slot);

      player.Dimension?.Broadcast(new LevelSoundEventPacket {
        Event = LevelSoundEvent.Break,
        Position = player.Position,
        Data = 0,
        ActorIdentifier = string.Empty,
        BabyMob = false,
        DisableRelativeVolume = false,
        UniqueActorId = 0,
        FireAtPosition = new Optional<Vec3f> { HasValue = false, Value = default }
      });
      return;
    }

    inventory?.Container.UpdateSlot(slot);
  }

  private void SyncDamageTag() {
    CompoundTag nbt = ItemStack.ExtraData?.Nbt ?? new CompoundTag();
    nbt.Set("Damage", new IntTag { Value = _damage });

    ItemStack.SetExtraData(new ItemInstanceUserData {
      Nbt = nbt,
      CanPlaceOn = ItemStack.ExtraData?.CanPlaceOn ?? [],
      CanDestroy = ItemStack.ExtraData?.CanDestroy ?? [],
      Ticking = ItemStack.ExtraData?.Ticking
    });
  }

  private bool HasUnbreakingProtection() {
    ItemStackEnchantmentTrait? enchantments = ItemStack.GetTrait<ItemStackEnchantmentTrait>();
    if (enchantments is null) {
      return false;
    }

    int unbreakingLevel = enchantments.GetLevel(17);
    if (unbreakingLevel <= 0) {
      return false;
    }

    return Random.Shared.Next(unbreakingLevel + 1) > 0;
  }

  private bool PassesDamageChance() {
    if (_damageChanceMin <= 0 && _damageChanceMax <= 0) {
      return true;
    }

    int chance = _damageChanceMax > _damageChanceMin
      ? Random.Shared.Next(_damageChanceMin, _damageChanceMax + 1)
      : _damageChanceMax;

    return Random.Shared.Next(100) < chance;
  }
}
