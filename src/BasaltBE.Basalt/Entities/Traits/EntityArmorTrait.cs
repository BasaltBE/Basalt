using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Items.Enchantments;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using BasaltBE.Nbt.Enums;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Entities.Traits;

[EntityTraitAppliesTo(Identifiers = ["minecraft:player"])]
public sealed class EntityArmorTrait : EntityTrait {
    public readonly EntityContainer Armor;

    public EntityArmorTrait(Entity entity) : base(entity) {
        Armor = new EntityContainer(
            entity,
            4,
            ContainerId.Armor,
            ContainerEnumName.Armor,
            ContainerType.Armor
        );
    }

    public override void OnSpawn() {
        if (Entity is Player player) {
            Armor.AddPersistent(player);
            SyncEquipment();
        }
    }

    public override void OnDespawn() {
        if (Entity is Player player)
            Armor.RemovePersistence(player);
    }

    public override void Read(CompoundTag entityTag) {
        if (entityTag.Get<ListTag>("Armor") is not { } items)
            return;

        foreach (Tag entry in items.Value) {
            if (entry is not CompoundTag item ||
                !item.TryGet("Slot", out ByteTag? slot) ||
                !item.TryGet("Name", out StringTag? name) ||
                !item.TryGet("Count", out ByteTag? count) ||
                slot!.Value >= Armor.ContainerSize ||
                ItemPalette.Find(name!.Value) is not { } type ||
                count!.Value == 0)
                continue;

            uint metadata = item.Get<ShortTag>("Damage") is { } damage
                ? unchecked((ushort)damage.Value)
                : 0U;
            Armor.LoadItem(slot.Value, new ItemStack(type, count.Value, metadata, item.Get<CompoundTag>("tag")));
        }
    }

    public override void Write(CompoundTag entityTag) {
        ListTag items = new() { ElementType = TagType.Compound };
        for (int slot = 0; slot < Armor.ContainerSize; slot++) {
            if (Armor.GetItem(slot) is not { } stack)
                continue;

            CompoundTag item = new();
            item.Add("Name", new StringTag { Value = stack.Type.Identifier });
            item.Add("Count", new ByteTag { Value = checked((byte)stack.StackSize) });
            item.Add("Damage", new ShortTag { Value = unchecked((short)stack.Metadata) });
            item.Add("Slot", new ByteTag { Value = checked((byte)slot) });
            if (stack.Nbt is not null)
                item.Add("tag", stack.Clone().Nbt!);
            items.Value.Add(item);
        }
        entityTag.Add("Armor", items);
    }

    public override void OnUseItem(ItemStack? item) {
        if (item is null || Entity is not Player player ||
            player.GetTrait<PlayerInventoryTrait>() is not { } inventory ||
            !ReferenceEquals(inventory.Inventory.GetItem(inventory.SelectedSlot), item))
            return;

        Equip(inventory, inventory.SelectedSlot);
    }

    internal bool Equip(PlayerInventoryTrait inventory, int inventorySlot) {
        if ((uint)inventorySlot >= inventory.Inventory.ContainerSize ||
            inventory.Inventory.GetItem(inventorySlot)?.Type.ArmorSlot is not ArmorSlot slot)
            return false;

        ItemStack? equipped = inventory.Inventory.TakeItem(inventorySlot, 1);
        if (equipped is null)
            return false;

        ItemStack? previous = Armor.GetItem((int)slot);
        Armor.SetItem((int)slot, equipped);
        if (previous is not null)
            inventory.Inventory.SetItem(inventorySlot, previous);
        SyncEquipment();
        return true;
    }

    public override void OnHurt(Events.Entities.EntityHurtEvent hurt) {
        if (hurt.Cause is EntityDamageCause.Drowning or EntityDamageCause.Void ||
            Entity is Player { GameType: GameType.Creative or GameType.Spectator })
            return;

        int durabilityDamage = Math.Max(1, (int)(hurt.Damage / 4));
        bool changed = false;
        for (int slot = 0; slot < Armor.ContainerSize; slot++) {
            ItemStack? item = Armor.GetItem(slot);
            if (item is null || item.Type.MaxDurability <= 0)
                continue;

            int previousDamage = item.Damage;
            item.DamageDurability(durabilityDamage);
            changed |= item.Damage != previousDamage || !ReferenceEquals(Armor.GetItem(slot), item);
        }

        if (changed)
            SyncEquipment();
    }

    public float ReduceDamage(float damage, EntityDamageCause cause) {
        if (cause is EntityDamageCause.Drowning or EntityDamageCause.Void)
            return damage;

        int armorProtection = 0;
        int enchantmentProtection = 0;
        for (int slot = 0; slot < Armor.ContainerSize; slot++) {
            ItemStack? item = Armor.GetItem(slot);
            if (item is null)
                continue;

            armorProtection += item.Type.ArmorProtection;
            enchantmentProtection += item.GetEnchantmentLevel(EnchantmentPalette.Protection);
        }

        damage *= 1 - Math.Clamp(armorProtection, 0, 20) * 0.04f;
        return damage * (1 - Math.Clamp(enchantmentProtection, 0, 20) * 0.04f);
    }

    public MobArmorEquipmentPacket CreateEquipmentPacket() => new() {
        TargetRuntimeId = Entity.RuntimeId,
        Head = Armor.GetItem((int)ArmorSlot.Head)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor(),
        Torso = Armor.GetItem((int)ArmorSlot.Chest)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor(),
        Legs = Armor.GetItem((int)ArmorSlot.Legs)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor(),
        Feet = Armor.GetItem((int)ArmorSlot.Feet)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor(),
        Body = new NetworkItemStackDescriptor()
    };

    public void SyncEquipment() {
        if (Entity.SpawnedDimension is not { } dimension)
            return;

        MobArmorEquipmentPacket packet = CreateEquipmentPacket();
        if (Entity is Player player)
            player.Network.SendPacket(player, packet);
        dimension.Broadcast(packet, Entity);
    }
}
