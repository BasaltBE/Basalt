using Basalt.Core;
using Basalt.Protocol.Types;

namespace Basalt.Events;

public sealed class PlayerBreakBlockSignal : PlayerSignal
{
    public override ServerEvent Event => ServerEvent.PlayerBreakBlock;
    public BlockPos BlockPosition { get; }
    public int BlockFace { get; }
    public bool Cancelled;

    public PlayerBreakBlockSignal(Player player, BlockPos blockPosition, int blockFace) : base(player)
    {
        BlockPosition = blockPosition;
        BlockFace = blockFace;
    }

    public bool Emit()
    {
        return !Cancelled;
    }

    public void Cancel()
    {
        Cancelled = true;
    }
}
