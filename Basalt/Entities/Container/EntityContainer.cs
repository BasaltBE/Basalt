namespace Basalt.Core.Entities.Container;

using Basalt.Core.Containers;


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

        if (Entity is Player.Player player && Identifier is not null && player.Spawned) {
            player.Send(new InventorySlotPacket {
                ContainerId = (byte)(Identifier ?? ContainerID.CONTAINER_ID_NONE),
                Slot = (uint)slot,
                // FullContainerName = new BedrockProtocol.Types.FullContainerName() {
                //     ContainerName = ContainerEnumName.SmithingTableInputContainer
                // },
                // Container = new Optional<FullContainerName> {
                //     HasValue = true,
                //     Value = new FullContainerName { ContainerId = 0 }
                // },
                Item = GetItem(slot)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor()
            });
        }

        base.UpdateSlot(slot);
    }

    public override void Update() {
        Entity.OnContainerUpdate(this);

        if (Entity is Player.Player player && Identifier is { } identifier && player.Spawned) {
            InventoryContentPacket packet = new() {
                ContainerId = (byte)identifier,
                Slots = new List<NetworkItemStackDescriptor>(GetSize()),
                FullContainerName = GetFullContainerName((ContainerID)identifier),
                StorageItem = new NetworkItemStackDescriptor()
            };

            for (int i = 0; i < GetSize(); i++) {
                packet.Slots.Add(GetItem(i)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor());
            }

            player.Send(packet);
            return;
        }

        base.Update();
    }

    protected override long GetContainerEntityUniqueId() {
        return Entity.UniqueId;
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
