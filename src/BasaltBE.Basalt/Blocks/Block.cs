using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Blocks.BlockActors.Types;
using BasaltBE.Nbt.Types;

namespace BasaltBE.Basalt.Blocks;

public sealed class Block {
    public BlockType Type;
    public BlockPermutation Permutation;
    public CompoundTag? Nbt;
    public BlockActor? Actor;

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

    public static BlockPermutation? Till(BlockPermutation permutation) => permutation.Identifier switch {
        "minecraft:grass_block" or "minecraft:dirt" or "minecraft:grass_path" => BlockPalette.FindDefaultPermutation("minecraft:farmland"),
        "minecraft:coarse_dirt" or "minecraft:dirt_with_roots" => BlockPalette.FindDefaultPermutation("minecraft:dirt"),
        _ => null
    };

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
