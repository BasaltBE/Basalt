namespace Basalt.Core.Events;

using Basalt.Core.Blocks;
using Basalt.Core.Item;
using Basalt.Core.Player;
using BedrockProtocol.Types;

public sealed class PlayerPlaceBlockSignal : PlayerSignal {
    public override ServerEvent Event => ServerEvent.PlayerPlaceBlock;
    public BlockPos BlockPosition { get; }
    public int BlockFace { get; }
    public BlockType BlockType { get; }
    public ItemStack Item { get; }
    public bool Cancelled;

    public PlayerPlaceBlockSignal(Player player, BlockPos blockPosition, int blockFace, BlockType blockType, ItemStack item) : base(player) {
        BlockPosition = blockPosition;
        BlockFace = blockFace;
        BlockType = blockType;
        Item = item;
    }

    public bool Emit() {
        return !Cancelled;
    }

    public void Cancel() {
        Cancelled = true;
    }
}






