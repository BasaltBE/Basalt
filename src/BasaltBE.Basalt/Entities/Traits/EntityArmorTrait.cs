using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Containers;
using BasaltBE.Basalt.Items.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

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
        if (hurt.Cause is EntityDamageCause.Drowning or EntityDamageCause.Void)
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

        int protection = 0;
        for (int slot = 0; slot < Armor.ContainerSize; slot++)
            protection += Armor.GetItem(slot)?.Type.ArmorProtection ?? 0;

        return damage * (1 - Math.Clamp(protection, 0, 20) * 0.04f);
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
