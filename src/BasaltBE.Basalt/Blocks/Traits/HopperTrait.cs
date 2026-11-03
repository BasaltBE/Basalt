using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo("minecraft:hopper")]
public sealed class HopperTrait : BlockTrait {
    public override bool OnPlace(Player player, int x, int y, int z, byte face, Vec3 clickPosition, ref BlockPermutation permutation) {
        if (face >= BlockDirections.OppositeByFace.Length)
            return false;

        int facing = BlockDirections.OppositeByFace[face];
        if (BlockPalette.Find(permutation.Identifier, permutation.State, "facing_direction", facing) is not { } faced)
            return false;

        permutation = faced;
        return true;
    }
}
