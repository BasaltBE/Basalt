namespace Basalt.Core.Entities.Container;

using Basalt.Core.Containers;
using Basalt.Core.Player.Traits;


using BedrockProtocol.Enums;
using BedrockProtocol.Packets;
using BedrockProtocol.Types;

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
                Logger.Info($"[Container] Player crafting slot {slot} -> UI slot {PlayerCraftingGridTrait.SlotOffset + slot}");
                player.Send(new InventorySlotPacket {
                    ContainerId = (byte)ContainerID.CONTAINER_ID_PLAYER_ONLY_UI,
                    Slot = (uint)(PlayerCraftingGridTrait.SlotOffset + slot),
                    Item = GetItem(slot)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor()
                });
                return;
            }

            if (Identifier is { } identifier) {
                player.Send(new InventorySlotPacket {
                    ContainerId = (byte)identifier,
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
                Logger.Info($"[Container] Player crafting resync slot {slot} -> UI slot {PlayerCraftingGridTrait.SlotOffset + slot}");
                player.Send(new InventorySlotPacket {
                    ContainerId = (byte)ContainerID.CONTAINER_ID_PLAYER_ONLY_UI,
                    Slot = (uint)(PlayerCraftingGridTrait.SlotOffset + slot),
                    Item = GetItem(slot)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor()
                });
            }
            return;
        }

        if (Entity is Player.Player playerWithIdentifier && Identifier is { } identifier && playerWithIdentifier.Spawned) {
            InventoryContentPacket packet = new() {
                ContainerId = (byte)identifier,
                Slots = new List<NetworkItemStackDescriptor>(GetSize()),
                FullContainerName = GetFullContainerName((ContainerID)identifier),
                StorageItem = new NetworkItemStackDescriptor()
            };

            for (int i = 0; i < GetSize(); i++) {
                packet.Slots.Add(GetItem(i)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor());
            }

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

    protected override bool CanOpen(Player.Player player, ContainerID containerId) {
        return true;
    }

    protected override ContainerEnumName GetFullContainerID() {
        if (Identifier == ContainerID.CONTAINER_ID_PLAYER_ONLY_UI) {
            return ContainerEnumName.BarrelContainer;
        }

        if (Type == ContainerType.ARMOR) {
            return ContainerEnumName.ArmorContainer;
        }

        return base.GetFullContainerID();
    }
}
