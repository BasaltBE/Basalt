using Basalt.Core.Blocks.Types;
using Basalt.Core.Worlds.Dimensions;

using BedrockProtocol.Types;
namespace Basalt.Core.Blocks.Traits;

public sealed class GrassBlockTrait : BlockTrait {
    public new static readonly string Identifier = "minecraft:grass_block";
    public new static readonly string[] Types = ["minecraft:grass_block"];

    private static readonly BlockPermutation ShortGrass =
        BlockPermutation.Resolve("minecraft:short_grass");
    private static readonly BlockPermutation Dandelion =
        BlockPermutation.Resolve("minecraft:dandelion");
    private static readonly BlockPermutation Poppy =
        BlockPermutation.Resolve("minecraft:poppy");

    public GrassBlockTrait(Block block) : base(block) {
    }

    public bool Fertilize(
        Dimension dimension,
        BlockPos position,
        Random? random = null) {
        ArgumentNullException.ThrowIfNull(dimension);

        if (Block.Type.Identifier != "minecraft:grass_block") {
            return false;
        }

        Random source = random ?? Random.Shared;
        for (int attempt = 0; attempt < 128; attempt++) {
            int x = position.X;
            int y = position.Y + 1;
            int z = position.Z;

            for (int step = 0; step < attempt / 16; step++) {
                x += source.Next(-1, 2);
                y += source.Next(-1, 2) * source.Next(3) / 2;
                z += source.Next(-1, 2);

                if (dimension.GetPermutation(x, y - 1, z).Type.Identifier !=
                    "minecraft:grass_block") {
                    break;
                }
            }

            if (!dimension.GetPermutation(x, y, z).Type.Air ||
                dimension.GetPermutation(x, y - 1, z).Type.Identifier !=
                "minecraft:grass_block") {
                continue;
            }

            float vegetation = source.NextSingle();
            BlockPermutation permutation = vegetation < 0.8f
                ? ShortGrass
                : vegetation < 0.9f
                    ? Dandelion
                    : Poppy;
            dimension.SetPermutation(x, y, z, permutation);
        }

        return true;
    }
}
