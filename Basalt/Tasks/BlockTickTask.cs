namespace Basalt.Core.Tasks;

using Basalt.Core.Worlds.Dimensions;
using Basalt.Protocol.Types;

internal sealed class BlockTickTask : DelayedTask {
    private readonly Dimension _dimension;

    internal BlockPos Position { get; }
    internal string BlockIdentifier { get; }

    public BlockTickTask(Dimension dimension, BlockPos position, string blockIdentifier, uint delay) {
        _dimension = dimension;
        Position = position;
        BlockIdentifier = blockIdentifier;
        DelayTicks = delay;
        RunOnMainThread = true;
    }

    public override void Execute() {
        _dimension.ExecuteBlockTick(this);
    }
}
