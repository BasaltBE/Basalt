namespace Basalt.Core.Events;

using Basalt.Core.Player;
using Basalt.Protocol.Types;

public sealed class PlayerStartBreakBlockSignal : PlayerSignal
{
    public override ServerEvent Event => ServerEvent.PlayerStartBreakBlock;
    public BlockPos BlockPosition { get; }
    public bool Cancelled;

    public PlayerStartBreakBlockSignal(Player player, BlockPos blockPosition) : base(player)
    {
        BlockPosition = blockPosition;
    }

    public bool Emit() => !Cancelled;

    public void Cancel()
    {
        Cancelled = true;
    }
}
