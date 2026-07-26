using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Types;

namespace Basalt.Core.Worlds.Dimensions.Generation.Features;

public sealed class TreeBlock {
    public readonly string Identifier;
    public readonly BlockState? State;

    public TreeBlock(string identifier, BlockState? state = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        Identifier = identifier;
        if (state is not null) {
            BlockState copy = [];
            foreach ((string key, BlockStateValue value) in state) {
                copy[key] = value;
            }

            State = copy;
        }
    }

    public BlockPermutation Resolve() {
        return BlockPermutation.Resolve(Identifier, State);
    }

    public bool Matches(BlockPermutation permutation) {
        return permutation.Type.Identifier == Identifier &&
            (State is null || permutation.Matches(State));
    }

    public static implicit operator TreeBlock(string identifier) {
        return new TreeBlock(identifier);
    }
}
