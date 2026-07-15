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

        if (Entity is Player.Player player && Identifier == 0 && player.Spawned)
        {
            player.Send(new InventorySlotPacket
            {
                WindowId = Identifier ?? 0,
                Slot = slot,
                Container = new Optional<FullContainerName>
                {
                    HasValue = true,
                    Value = new FullContainerName { ContainerId = (byte)ContainerId.Inventory }
                },
                NewItem = GetItem(slot)?.ToNetworkStackDescriptor() ?? new NetworkItemStackDescriptor()
            });
        }

        base.UpdateSlot(slot);
    }

    public override void Update()
    {
        Entity.OnContainerUpdate(this);
        base.Update();
    }

    protected override long GetContainerEntityUniqueId()
    {
        return Entity.UniqueId;
    }

    protected override Basalt.Protocol.Types.BlockPos GetContainerPosition()
    {
        if (Entity is Player.Player)
        {
            return new Basalt.Protocol.Types.BlockPos
            {
                X = 0,
                Y = 0,
                Z = 0
            };
        }

        return new Basalt.Protocol.Types.BlockPos
        {
            X = (int)MathF.Floor(Entity.Position.X),
            Y = (int)MathF.Floor(Entity.Position.Y),
            Z = (int)MathF.Floor(Entity.Position.Z)
        };
    }

    // TODO: Add proper checks, e.g container already opened
    // Or if something is preventing it from opening
    protected override bool CanOpen(Player.Player player, int windowId)
    {
        return true;
    }

    protected override byte GetFullContainerNameId()
    {
        if (Identifier == 124)
        {
            return 0x3A;
        }

        return base.GetFullContainerNameId();
    }
}






