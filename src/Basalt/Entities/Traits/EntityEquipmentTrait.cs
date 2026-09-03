namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Containers;
using Basalt.Core.Entities.Container;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Item;
using Basalt.BedrockProtocol.NBT;

using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public sealed class EntityEquipmentTrait : EntityTrait {
    public static new readonly EntityIdentifier[] Types = [EntityIdentifier.Player];
    public static new readonly string[] Components = ["minecraft:equipment"];

    public EntityContainer Armor { get; }
    public EntityContainer Offhand { get; }

    public EntityEquipmentTrait(Entity entity) : base(entity) {
        Armor = new EntityContainer(entity, ContainerType.ARMOR, 4) {
            Identifier = ContainerId.Armor
        };

        Offhand = new EntityContainer(entity, ContainerType.INVENTORY, 1) {
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
    }

    public void SyncToPlayer(Player.Player player) {
        SendContainerContent(player, Armor);
        SendContainerContent(player, Offhand);
    }

    private static void SendContainerContent(Player.Player player, EntityContainer container) {
        InventoryContentPacket packet = new() {
            ContainerId = container.Identifier ?? ContainerId.None,
            Slots = Enumerable.Range(0, container.GetSize())
                .Select(i => container.GetItem(i)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor())
                .ToArray(),
            Container = new FullContainerName { ContainerName = 0 },
            StorageItem = new NetworkItemStackDescriptor()
        };

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
