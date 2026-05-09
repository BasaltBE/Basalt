using Basalt.Containers;

namespace Basalt.Entity.Container;

public sealed class EntityContainer : Containers.Container
{
    public Entity Entity { get; }

    public EntityContainer(Entity entity, ContainerType type, int size) : base(type, size)
    {
        Entity = entity;
    }

    public bool IsOwnedBy(Core.Player player)
    {
        return ReferenceEquals(Entity, player);
    }

    public override void SetItem(int slot, Basalt.Item.ItemStack item)
    {
        base.SetItem(slot, item);
    }

    public override void Update()
    {
        Entity.OnContainerUpdate(this);
        base.Update();

        if (Containers.Container.IsPacketSuppressed())
        {
            return;
        }

        if (Entity is Core.Player player && player.HasSpawned)
        {
            bool isOccupant = GetAllOccupants().Any(entry => ReferenceEquals(entry.Key, player));
            if (!isOccupant)
            {
                SendContentTo(player, Identifier ?? 0);
            }
        }
    }

    public override void UpdateSlot(int slot)
    {
        Entity.OnContainerUpdate(this);
        base.UpdateSlot(slot);

        if (Containers.Container.IsPacketSuppressed())
        {
            return;
        }

        if (Entity is Core.Player player && player.HasSpawned)
        {
            bool isOccupant = GetAllOccupants().Any(entry => ReferenceEquals(entry.Key, player));
            if (!isOccupant)
            {
                SendSlotTo(player, Identifier ?? 0, slot);
            }
        }
    }

    protected override long GetContainerEntityUniqueId()
    {
        return (long)Entity.RuntimeId;
    }

    protected override Basalt.Protocol.Types.BlockPos GetContainerPosition()
    {
        if (Entity is Core.Player)
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

    protected override bool ShouldSendContainerOpen(Core.Player player, int windowId)
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
