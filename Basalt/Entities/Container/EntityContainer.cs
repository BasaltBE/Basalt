namespace Basalt.Core.Entities.Container;

using Basalt.Core.Containers;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

public sealed class EntityContainer : Containers.Container
{
    public Entity Entity { get; }

    public EntityContainer(Entity entity, ContainerType type, int size) : base(type, size)
    {
        Entity = entity;
    }

    public bool IsOwnedBy(Player.Player player)
    {
        return ReferenceEquals(Entity, player);
    }

    public override void SetItem(int slot, Basalt.Core.Item.ItemStack item)
    {
        base.SetItem(slot, item);
    }

    public override void UpdateSlot(int slot)
    {
        Entity.OnContainerUpdate(this);
        if (slot < 0 || slot >= GetSize())
        {
            base.UpdateSlot(slot);
            return;
        }

        if (Entity is Player.Player player && Identifier is not null && player.Spawned)
        {
            player.Send(new InventorySlotPacket
            {
                ContainerId = Identifier ?? ContainerId.None,
                Slot = slot,
                Container = new Optional<FullContainerName>
                {
                    HasValue = true,
                    Value = new FullContainerName { ContainerId = 0 }
                },
                NewItem = GetItem(slot)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor()
            });
        }

        base.UpdateSlot(slot);
    }

    public override void Update()
    {
        Entity.OnContainerUpdate(this);

        if (Entity is Player.Player player && Identifier is not null && player.Spawned)
        {
            InventoryContentPacket packet = new()
            {
                ContainerId = Identifier ?? ContainerId.None,
                Content = new List<NetworkItemStackDescriptor>(GetSize()),
                Container = new FullContainerName { ContainerId = 0 },
                StorageItem = new NetworkItemStackDescriptor()
            };

            for (int i = 0; i < GetSize(); i++)
            {
                packet.Content.Add(GetItem(i)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor());
            }

            player.Send(packet);
            return;
        }

        base.Update();
    }

    protected override long GetContainerEntityUniqueId()
    {
        return Entity.UniqueId;
    }

    protected override BlockPos GetContainerPosition()
    {
        if (Entity is Player.Player)
        {
            return new BlockPos
            {
                X = 0,
                Y = 0,
                Z = 0
            };
        }

        return new BlockPos
        {
            X = (int)MathF.Floor(Entity.Position.X),
            Y = (int)MathF.Floor(Entity.Position.Y),
            Z = (int)MathF.Floor(Entity.Position.Z)
        };
    }

    protected override bool CanOpen(Player.Player player, ContainerId containerId)
    {
        return true;
    }

    protected override byte GetFullContainerId()
    {
        if (Identifier == ContainerId.Ui)
        {
            return (byte)ContainerName.Barrel;
        }

        if (Type == ContainerType.Armor)
        {
            return (byte)ContainerName.Armor;
        }

        return base.GetFullContainerId();
    }
}
