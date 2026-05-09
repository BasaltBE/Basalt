using Basalt.Containers;
using Basalt.World.Dimension;

namespace Basalt.Block.Container;

public sealed class BlockContainer : Containers.Container
{
    public Dimension Dimension { get; }
    public int X { get; }
    public int Y { get; }
    public int Z { get; }
    public Action<BlockContainer>? OnContainerUpdated { get; set; }

    public BlockContainer(Dimension dimension, int x, int y, int z, ContainerType type, int size) : base(type, size)
    {
        Dimension = dimension;
        X = x;
        Y = y;
        Z = z;
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

    protected override Protocol.Types.BlockPos GetContainerPosition()
    {
        return new Protocol.Types.BlockPos
        {
            X = X,
            Y = Y,
            Z = Z
        };
    }
}
