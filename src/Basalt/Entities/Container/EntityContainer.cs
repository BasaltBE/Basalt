namespace Basalt.Core.Entities.Container;

using Basalt.Core.Containers;
using Basalt.Core.Player.Traits;


using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public sealed class EntityContainer : Containers.Container {
    public Entity Entity { get; }

    public EntityContainer(Entity entity, ContainerType type, int size) : base(type, size) {
        Entity = entity;
    }

    public bool IsOwnedBy(Player.Player player) {
        return ReferenceEquals(Entity, player);
    }

    public override void SetItem(int slot, Basalt.Core.Item.ItemStack item) {
        base.SetItem(slot, item);
    }

    public override void UpdateSlot(int slot) {
        Entity.OnContainerUpdate(this);
        if (slot < 0 || slot >= GetSize()) {
            base.UpdateSlot(slot);
            return;
        }

        if (Entity is Player.Player player && player.Spawned) {
            bool playerCraftingGrid = Identifier is null && Type == ContainerType.NONE;
            if (playerCraftingGrid) {
                player.Send(new InventorySlotPacket {
                    ContainerId = ContainerId.PlayerOnlyUi,
                    Slot = (uint)(PlayerCraftingGridTrait.SlotOffset + slot),
                    Item = GetItem(slot)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor()
                });
                return;
            }

            if (Identifier is { } identifier) {
                player.Send(new InventorySlotPacket {
                    ContainerId = identifier,
                    Slot = (uint)slot,
                    Item = GetItem(slot)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor()
                });
                return;
            }
        }

        base.UpdateSlot(slot);
    }

    public override void Update() {
        Entity.OnContainerUpdate(this);

        if (Entity is Player.Player player && player.Spawned && Identifier is null && Type == ContainerType.NONE) {
            for (int slot = 0; slot < GetSize(); slot++) {
                player.Send(new InventorySlotPacket {
                    ContainerId = ContainerId.PlayerOnlyUi,
                    Slot = (uint)(PlayerCraftingGridTrait.SlotOffset + slot),
                    Item = GetItem(slot)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor()
                });
            }
            return;
        }

        if (Entity is Player.Player playerWithIdentifier && Identifier is { } identifier && playerWithIdentifier.Spawned) {
            InventoryContentPacket packet = new() {
                ContainerId = identifier,
                Slots = Enumerable.Range(0, GetSize()).Select(i => GetItem(i)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor()).ToArray(),
                Container = GetFullContainerName((ContainerId)identifier),
                StorageItem = new NetworkItemStackDescriptor()
            };

            playerWithIdentifier.Send(packet);
            return;
        }

        base.Update();
    }

    protected override long GetContainerEntityUniqueId() {
        return Entity.UniqueId;
    }

    protected override int GetNetworkSlot(int slot) {
        return slot;
    }

    protected override BlockPos GetContainerPosition() {
        if (Entity is Player.Player) {
            return new BlockPos {
                X = 0,
                Y = 0,
                Z = 0
            };
        }

        return new BlockPos {
            X = (int)MathF.Floor(Entity.Position.X),
            Y = (int)MathF.Floor(Entity.Position.Y),
            Z = (int)MathF.Floor(Entity.Position.Z)
        };
    }

    protected override bool CanOpen(Player.Player player, ContainerId containerId) {
        return true;
    }

    protected override ContainerEnumName GetFullContainerId() {
        if (Identifier == ContainerId.PlayerOnlyUi) {
            return ContainerEnumName.BarrelContainer;
        }

        if (Type == ContainerType.ARMOR) {
            return ContainerEnumName.ArmorContainer;
        }

        return base.GetFullContainerId();
    }
}
