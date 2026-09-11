namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Traits.Types;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Worlds.Dimensions;
using Basalt.BedrockProtocol.Types;

public sealed class RedstoneLampTrait : BlockTrait {
    public static new readonly string[] Types = [BlockIdentifier.RedstoneLamp.ToIdentifier(), BlockIdentifier.LitRedstoneLamp.ToIdentifier()];

    public RedstoneLampTrait(Block block) : base(block) {
    }

    public override void OnPlace(BlockPlaceDetails details) {
        Dimension dimension = details.Player.Dimension!;
        SetLit(dimension, details.BlockPosition, RedstonePower.GetDirectSignal(dimension, details.BlockPosition) > 0);
    }

    public override void OnRedstoneUpdate(BlockTickDetails details) {
        bool lit = Block.Type.Identifier == BlockIdentifier.LitRedstoneLamp.ToIdentifier();
        bool powered = RedstonePower.GetDirectSignal(details.Dimension, details.BlockPosition) > 0;
        if (!lit && powered) SetLit(details.Dimension, details.BlockPosition, true);
        else if (lit && !powered) details.Dimension.ScheduleBlockTick(details.BlockPosition, 4);
    }

    public override void OnTick(BlockTickDetails details) {
        if (Block.Type.Identifier == BlockIdentifier.LitRedstoneLamp.ToIdentifier() &&
            RedstonePower.GetDirectSignal(details.Dimension, details.BlockPosition) == 0) {
            SetLit(details.Dimension, details.BlockPosition, false);
        }
    }

    private static void SetLit(Dimension dimension, BlockPos position, bool lit) {
        dimension.SetPermutation(position.X, position.Y, position.Z, BlockPermutation.Resolve(lit ? BlockIdentifier.LitRedstoneLamp : BlockIdentifier.RedstoneLamp));
    }
}
