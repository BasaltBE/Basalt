namespace Basalt.Server.Block.Container;

using Basalt.Server.Containers;
using Basalt.Protocol.Types;
using Basalt.Server.World.Dimension;


public sealed class BlockContainer : Containers.Container
{
    public Action<BlockContainer, Basalt.Server.Player.Player>? OnViewerAddedEvent { get; set; }
    public Action<BlockContainer, Basalt.Server.Player.Player>? OnViewerRemovedEvent { get; set; }
    public Dimension? Dimension { get; set; }
    public BlockPos Position { get; set; }
    public Action<BlockContainer>? OnContainerUpdated { get; set; }

    public BlockContainer(Dimension? dimension, BlockPos position, ContainerType type, int size) : base(type, size)
    {
        Dimension = dimension;
        Position = position;
    }

    public override void Update()
    {
        OnContainerUpdated?.Invoke(this);
        base.Update();
    }

    public override void UpdateSlot(int slot)
    {
        OnContainerUpdated?.Invoke(this);
        base.UpdateSlot(slot);
    }

    protected override BlockPos GetContainerPosition()
    {
        return Position;
    }

    protected override void OnViewerAdded(Basalt.Server.Player.Player player, int windowId)
    {
        OnViewerAddedEvent?.Invoke(this, player);
    }

    protected override void OnViewerRemoved(Basalt.Server.Player.Player player, int windowId)
    {
        OnViewerRemovedEvent?.Invoke(this, player);
    }
}







