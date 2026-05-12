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

    public override void UpdateSlot(int slot)
    {
        Entity.OnContainerUpdate(this);
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

    // TODO: Add proper checks, e.g container already opened
    // Or if something is preventing it from opening
    protected override bool CanOpen(Core.Player player, int windowId)
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
