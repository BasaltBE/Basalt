namespace Basalt.Core.Item.Traits;

using Basalt.Core.Entities.Traits;
using Basalt.Core.Item.Components;
using Basalt.Core.Item.Traits.Types;
using Basalt.Core.Player;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.NBT;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public sealed class ItemStackDurabilityTrait : ItemTrait {
    public new static string Identifier => "durability";
    public new static readonly System.Type? Component = typeof(ItemTypeDurabilityComponent);

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

            player.Dimension?.PlaySound(
                "break",
                player.Position);
            return;
        }

        inventory?.Container.UpdateSlot(slot);
    }

    private void SyncDamageTag() {
        CompoundTag nbt = ItemStack.Storage ?? new CompoundTag();
        nbt.Set("Damage", new IntTag { Value = _damage });

        ItemStack.Storage = nbt;
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
