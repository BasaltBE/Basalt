namespace Basalt.Core.Blocks.Traits;

using Basalt.Core.Blocks.Types;
using Basalt.Core.Worlds.Dimensions;
using Basalt.BedrockProtocol.Types;

public sealed class FlowerTrait : BlockTrait {
    public new static readonly string Identifier = "minecraft:flower";
    public new static readonly string[] Types = [
        BlockIdentifier.Dandelion.ToIdentifier(),
        BlockIdentifier.Poppy.ToIdentifier(),
        BlockIdentifier.BlueOrchid.ToIdentifier(),
        BlockIdentifier.RedTulip.ToIdentifier(),
        BlockIdentifier.PinkTulip.ToIdentifier(),
        BlockIdentifier.WhiteTulip.ToIdentifier(),
        BlockIdentifier.OrangeTulip.ToIdentifier(),
        BlockIdentifier.Allium.ToIdentifier(),
        BlockIdentifier.AzureBluet.ToIdentifier(),
        BlockIdentifier.OxeyeDaisy.ToIdentifier(),
        BlockIdentifier.Cornflower.ToIdentifier(),
        BlockIdentifier.LilyOfTheValley.ToIdentifier(),
    ];
    public List<BlockPos> AffectedPositions { get; } = [];

    public FlowerTrait(Block block) : base(block) {
    }

    public bool Fertilize(Dimension dimension, BlockPos position, Random? random = null) {
        ArgumentNullException.ThrowIfNull(dimension);

        Random source = random ?? Random.Shared;
        BlockPermutation flower = Block.Permutation;
        int flowers = source.Next(1, 5);
        int placed = 0;
        AffectedPositions.Clear();

        for (int attempt = 0; attempt < 128 && placed < flowers; attempt++) {
            int x = position.X + source.Next(-3, 4);
            int y = position.Y + source.Next(-1, 2);
            int z = position.Z + source.Next(-3, 4);

            if (!dimension.GetPermutation(x, y, z).Type.Air ||
                dimension.GetPermutation(x, y - 1, z).Type.Identifier !=
                BlockIdentifier.GrassBlock.ToIdentifier()) {
                continue;
            }

            dimension.SetPermutation(x, y, z, flower);
            AffectedPositions.Add(new BlockPos { X = x, Y = y, Z = z });
            placed++;
        }

        return placed > 0;
    }
}
