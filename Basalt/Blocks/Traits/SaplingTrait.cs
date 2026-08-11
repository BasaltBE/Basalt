using Basalt.Core.Blocks.Types;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Worlds.Dimensions.Generation.Features;
using BedrockProtocol.Types;

namespace Basalt.Core.Blocks.Traits;

public sealed class SaplingTrait : BlockTrait {
    public new static readonly string Identifier = "minecraft:sapling";
    public new static readonly string[] Types = [
        "minecraft:oak_sapling",
        "minecraft:spruce_sapling",
        "minecraft:birch_sapling",
        "minecraft:jungle_sapling",
        "minecraft:acacia_sapling",
        "minecraft:dark_oak_sapling",
        "minecraft:cherry_sapling",
        "minecraft:pale_oak_sapling"
    ];

    public SaplingTrait(Block block) : base(block) {
    }

    public bool Fertilize(Dimension dimension, BlockPos position, Random? random = null) {
        ArgumentNullException.ThrowIfNull(dimension);

        if (!Block.Permutation.State.TryGetValue("age_bit", out BlockStateValue age) ||
            age.Kind != 2) {
            return false;
        }

        if (!age.AsBool()) {
            BlockState state = [];
            foreach ((string key, BlockStateValue value) in Block.Permutation.State) {
                state[key] = value;
            }

            state["age_bit"] = true;
            dimension.SetPermutation(
                position.X,
                position.Y,
                position.Z,
                Block.Type.GetPermutation(state));
            return true;
        }

        Grow(dimension, position, random);
        return true;
    }

    public bool Grow(Dimension dimension, BlockPos position, Random? random = null) {
        ArgumentNullException.ThrowIfNull(dimension);

        Random source = random ?? Random.Shared;
        string identifier = Block.Type.Identifier;
        BlockPos origin = position;
        TreeFeature? feature = identifier switch {
            "minecraft:oak_sapling" =>
                Trees.Require("minecraft:random_oak_tree_from_sapling_feature"),
            "minecraft:birch_sapling" => Trees.Birch,
            "minecraft:acacia_sapling" => Trees.Acacia,
            "minecraft:cherry_sapling" => Trees.Cherry,
            _ => null
        };

        if (identifier is "minecraft:spruce_sapling" or "minecraft:jungle_sapling") {
            bool square = FindSquare(dimension, position, identifier, out origin);
            feature = identifier == "minecraft:spruce_sapling"
                ? square ? Trees.MegaSpruce : Trees.Spruce
                : square ? Trees.MegaJungle : Trees.Jungle;
        }
        else if (identifier == "minecraft:dark_oak_sapling") {
            FindSquare(dimension, position, identifier, out origin);
            feature = Trees.DarkOak;
        }
        else if (identifier == "minecraft:pale_oak_sapling") {
            if (!FindSquare(dimension, position, identifier, out origin)) {
                return false;
            }

            feature = Trees.PaleOak;
        }

        return feature?.Populate(
            dimension,
            origin.X,
            origin.Y,
            origin.Z,
            source) == true;
    }

    private static bool FindSquare(
        Dimension dimension,
        BlockPos position,
        string identifier,
        out BlockPos origin) {
        for (int offsetX = -1; offsetX <= 0; offsetX++) {
            for (int offsetZ = -1; offsetZ <= 0; offsetZ++) {
                int x = position.X + offsetX;
                int z = position.Z + offsetZ;
                bool matches = true;

                for (int x2 = 0; x2 < 2 && matches; x2++) {
                    for (int z2 = 0; z2 < 2; z2++) {
                        if (dimension.GetPermutation(x + x2, position.Y, z + z2)
                            .Type.Identifier != identifier) {
                            matches = false;
                            break;
                        }
                    }
                }

                if (matches) {
                    origin = new BlockPos { X = x, Y = position.Y, Z = z };
                    return true;
                }
            }
        }

        origin = position;
        return false;
    }
}
