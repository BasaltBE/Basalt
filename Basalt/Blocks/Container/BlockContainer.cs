namespace Basalt.Core.Blocks.Container;

using Basalt.Core.Containers;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using Basalt.Core.Worlds.Dimensions;


public sealed class BlockContainer : Containers.Container
{
    public Action<BlockContainer, Basalt.Core.Player.Player>? OnViewerAddedEvent { get; set; }
    public Action<BlockContainer, Basalt.Core.Player.Player>? OnViewerRemovedEvent { get; set; }
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

    protected override void OnViewerAdded(Basalt.Core.Player.Player player, ContainerId containerId)
    {
        OnViewerAddedEvent?.Invoke(this, player);
    }

    protected override void OnViewerRemoved(Basalt.Core.Player.Player player, ContainerId containerId)
    {
        OnViewerRemovedEvent?.Invoke(this, player);
    }
}







