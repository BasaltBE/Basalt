namespace Basalt.Core.Events;

using Basalt.Core.Player;
using Basalt.Protocol.Types;

public sealed class PlayerInteractBlockSignal : PlayerSignal {
    public override ServerEvent Event => ServerEvent.PlayerInteractBlock;
    public BlockPos BlockPosition { get; }
    public bool Cancelled;

    public PlayerInteractBlockSignal(Player player, BlockPos blockPosition) : base(player) {
        BlockPosition = blockPosition;
    }

    public bool Emit() => !Cancelled;

    public void Cancel() {
        Cancelled = true;
    }
}
