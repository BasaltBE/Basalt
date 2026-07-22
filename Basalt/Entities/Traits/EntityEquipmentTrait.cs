namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Containers;
using Basalt.Core.Entities.Container;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Item;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

public sealed class EntityEquipmentTrait : EntityTrait {
    public static new readonly EntityIdentifier[] Types = [EntityIdentifier.Player];
    public static new readonly string[] Components = ["minecraft:equipment"];

    public EntityContainer Armor { get; }
    public EntityContainer Offhand { get; }

    public EntityEquipmentTrait(Entity entity) : base(entity) {
        Armor = new EntityContainer(entity, ContainerType.Armor, 4) {
            Identifier = ContainerId.Armor
        };

        Offhand = new EntityContainer(entity, ContainerType.Inventory, 1) {
            Identifier = ContainerId.Offhand
        };
    }

    public override EntityTrait Clone(Entity entity) {
        EntityEquipmentTrait clone = new(entity);
        for (int i = 0; i < Armor.GetSize(); i++) {
            ItemStack? item = Armor.GetItem(i);
            if (item is not null) {
                clone.Armor.SetItem(i, item);
            }
        }

        ItemStack? offhandItem = Offhand.GetItem(0);
        if (offhandItem is not null) {
            clone.Offhand.SetItem(0, offhandItem);
        }

        return clone;
    }

    public override void OnSpawn(EntitySpawnOptions details) {
        if (Entity is not Player.Player player) {
            return;
        }

        SyncToPlayer(player);
    }

    public void SyncToPlayer(Player.Player player) {
        SendContainerContent(player, Armor);
        SendContainerContent(player, Offhand);
    }

    private static void SendContainerContent(Player.Player player, EntityContainer container) {
        InventoryContentPacket packet = new() {
            ContainerId = container.Identifier ?? ContainerId.None,
            Content = new List<NetworkItemStackDescriptor>(container.GetSize()),
            Container = new FullContainerName { ContainerId = 0 },
            StorageItem = new NetworkItemStackDescriptor()
        };

        for (int i = 0; i < container.GetSize(); i++) {
            packet.Content.Add(container.GetItem(i)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor());
        }

        player.Send(packet);
    }

    public override void OnRead(CompoundTag tag) {
        ListTag? armorTag = tag.Get<ListTag>("armor");
        if (armorTag is not null) {
            Armor.Clear();
            for (int i = 0; i < armorTag.Values.Count && i < Armor.GetSize(); i++) {
                if (armorTag.Values[i] is not CompoundTag itemTag) {
                    continue;
                }

                ItemStack? item = ItemStack.Deserialize(itemTag);
                if (item is not null && item.StackSize > 0) {
                    Armor.SetItem(i, item);
                }
            }
        }

        CompoundTag? offhandTag = tag.Get<CompoundTag>("offhand");
        if (offhandTag is not null) {
            Offhand.Clear();
            ItemStack? item = ItemStack.Deserialize(offhandTag);
            if (item is not null && item.StackSize > 0) {
                Offhand.SetItem(0, item);
            }
        }
    }

    public override void OnWrite(CompoundTag tag) {
        ListTag armorTag = new() { Name = "armor" };
        for (int i = 0; i < Armor.GetSize(); i++) {
            ItemStack? armor = Armor.GetItem(i);
            armorTag.Values.Add(armor is null ? new CompoundTag() : armor.Serialize());
        }

        tag.Set("armor", armorTag);

        ItemStack? offhandItem = Offhand.GetItem(0);
        tag.Set("offhand", offhandItem is null ? new CompoundTag() : offhandItem.Serialize());
    }
}
