using BasaltBE.Basalt.Blocks.Types;

namespace BasaltBE.Basalt.Blocks;

public sealed class Block {
    public BlockType Type;
    public BlockPermutation Permutation;

    public Block(string identifier) {
        Type = BlockPalette.FindType(identifier)
            ?? throw new KeyNotFoundException($"Block type '{identifier}' was not found.");
        Permutation = BlockPalette.FindDefaultPermutation(identifier)
            ?? throw new KeyNotFoundException($"Default permutation for '{identifier}' was not found.");
    }

    public Block(BlockType type, BlockPermutation permutation) {
        Type = type;
        Permutation = permutation;
    }

    public bool SetStates(string name, object value) {
        BlockPermutation? permutation = BlockPalette.Find(Type.Identifier, Permutation.State, name, value);
        if (permutation is null) {
            return false;
        }

        Permutation = permutation;
        return true;
    }

    public bool SetStates(BlockState state) {
        BlockPermutation? permutation = BlockPalette.Find(Type.Identifier, Permutation.State, state);
        if (permutation is null) {
            return false;
        }

        Permutation = permutation;
        return true;
    }
}
