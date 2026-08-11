namespace Basalt.Core.Events;

using Basalt.Core.Blocks;
using Basalt.Core.Item;
using Basalt.Core.Player;
using BedrockProtocol.Types;

public sealed class PlayerBreakBlockSignal : PlayerSignal {
    public override ServerEvent Event => ServerEvent.PlayerBreakBlock;
    public BlockPos BlockPosition { get; }
    public int BlockFace { get; }
    public Block Block { get; }
    public ItemStack? Item { get; }
    public List<ItemStack>? Drops { get; set; }
    public BlockPermutation? Replacement { get; set; }
    public bool Cancelled;

    public PlayerBreakBlockSignal(Player player, BlockPos blockPosition, int blockFace, Block block, ItemStack? item) : base(player) {
        BlockPosition = blockPosition;
        BlockFace = blockFace;
        Block = block;
        Item = item;
    }

    public bool Emit() {
        return !Cancelled;
    }

    public void Cancel() {
        Cancelled = true;
    }
}






