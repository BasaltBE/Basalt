namespace Basalt.Core.Events;

using Basalt.Core.Blocks;
using Basalt.Core.Worlds.Dimensions;
using BedrockProtocol.Types;

/// <summary>
/// Emitted when a block forms naturally (e.g. cobblestone from lava+water).
/// The permutation can be replaced before it's applied.
/// </summary>
public sealed class BlockFormSignal : ISignal {
    public ServerEvent Event => ServerEvent.BlockForm;
    public Dimension Dimension { get; }
    public BlockPos Position { get; }
    public BlockPermutation Permutation { get; set; }
    public bool Cancelled { get; private set; }

    public BlockFormSignal(Dimension dimension, BlockPos position, BlockPermutation permutation) {
        Dimension = dimension;
        Position = position;
        Permutation = permutation;
    }

    public void Cancel() {
        Cancelled = true;
    }
}
