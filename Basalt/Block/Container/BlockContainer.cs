using Basalt.Containers;
using Basalt.Protocol.Types;
using Basalt.World.Dimension;

namespace Basalt.Block.Container;

public sealed class BlockContainer : Containers.Container
{
    public Action<BlockContainer, Basalt.Core.Player>? OnViewerAddedEvent { get; set; }
    public Action<BlockContainer, Basalt.Core.Player>? OnViewerRemovedEvent { get; set; }
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

    protected override void OnViewerAdded(Basalt.Core.Player player, int windowId)
    {
        OnViewerAddedEvent?.Invoke(this, player);
    }

    protected override void OnViewerRemoved(Basalt.Core.Player player, int windowId)
    {
        OnViewerRemovedEvent?.Invoke(this, player);
    }
}
