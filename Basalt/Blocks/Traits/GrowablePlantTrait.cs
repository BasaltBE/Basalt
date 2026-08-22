using Basalt.Core.Blocks.Types;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Worlds.Dimensions.Generation.Features;
using Basalt.BedrockProtocol.Types;

namespace Basalt.Core.Blocks.Traits;

public sealed class GrowablePlantTrait : BlockTrait {
    public new static readonly string Identifier = "minecraft:growable_plant";
    public new static readonly string[] Types = [
        "minecraft:crimson_fungus",
        "minecraft:warped_fungus",
        "minecraft:mangrove_propagule"
    ];

    public GrowablePlantTrait(Block block) : base(block) {
    }

    public bool Fertilize(
        Dimension dimension,
        BlockPos position,
        Random? random = null) {
        ArgumentNullException.ThrowIfNull(dimension);

        Random source = random ?? Random.Shared;
        string identifier = Block.Type.Identifier;
        if (identifier == "minecraft:mangrove_propagule") {
            return FertilizePropagule(dimension, position, source);
        }

        HugeFungusFeature feature;
        string baseBlock;
        if (identifier == "minecraft:crimson_fungus") {
            feature = Trees.CrimsonFungus;
            baseBlock = "minecraft:crimson_nylium";
        }
        else if (identifier == "minecraft:warped_fungus") {
            feature = Trees.WarpedFungus;
            baseBlock = "minecraft:warped_nylium";
        }
        else {
            return false;
        }

        if (dimension.GetPermutation(
            position.X,
            position.Y - 1,
            position.Z).Type.Identifier != baseBlock) {
            return false;
        }

        if (source.NextSingle() < 0.4f) {
            feature.Populate(
                dimension,
                position.X,
                position.Y,
                position.Z,
                source);
        }

        return true;
    }

    private bool FertilizePropagule(
        Dimension dimension,
        BlockPos position,
        Random random) {
        BlockState state = Block.Permutation.State;
        bool hanging =
            state.TryGetValue("hanging", out BlockStateValue hangingState) &&
            hangingState.Kind == 2 &&
            hangingState.AsBool();
        if (!hanging) {
            Trees.Mangrove.Populate(
                dimension,
                position.X,
                position.Y,
                position.Z,
                random);
            return true;
        }

        int stage =
            state.TryGetValue(
                "propagule_stage",
                out BlockStateValue stageState) &&
            stageState.Kind == 0
                ? (int)stageState.AsNumber()
                : 0;
        if (stage >= 4) {
            return false;
        }

        BlockState next = [];
        foreach ((string key, BlockStateValue value) in state) {
            next[key] = value;
        }

        next["propagule_stage"] = stage + 1;
        dimension.SetPermutation(
            position.X,
            position.Y,
            position.Z,
            Block.Type.GetPermutation(next));
        return true;
    }
}
