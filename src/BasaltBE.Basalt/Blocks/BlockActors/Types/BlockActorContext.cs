using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Nbt.Types;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.BlockActors.Types;

public readonly struct BlockActorContext(
    Dimension dimension,
    BlockPos position,
    string identifier,
    BlockPermutation permutation,
    CompoundTag nbt
) {
    public readonly Dimension Dimension = dimension;
    public readonly BlockPos Position = position;
    public readonly string Identifier = identifier;
    public readonly BlockPermutation Permutation = permutation;
    public readonly CompoundTag Nbt = nbt;
}
