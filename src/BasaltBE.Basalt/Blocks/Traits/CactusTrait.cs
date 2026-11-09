using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo("minecraft:cactus")]
public sealed class CactusTrait : BlockSupportTrait {
    static readonly BlockPermutation Cactus = BlockPalette.FindDefaultPermutation("minecraft:cactus")!;
    internal override bool HasRandomTicks => true;

    protected override bool? HasSupport(Dimension dimension, int x, int y, int z) {
        if (y <= dimension.MinimumY || y >= dimension.MaximumY) return false;

        if (dimension.GetPermutation(x, y + 1, z) is { Identifier: "minecraft:water" or "minecraft:flowing_water" or "minecraft:lava" or "minecraft:flowing_lava" })
            return false;

        for (byte face = 2; face < 6; face++) {
            (int offsetX, int offsetY, int offsetZ) = BlockDirections.OffsetByFace[face];
            BlockPermutation neighbor = dimension.GetPermutation(x + offsetX, y + offsetY, z + offsetZ);
            if (BlockPalette.FindType(neighbor.Identifier)?.Solid == true ||
                neighbor.Identifier is "minecraft:lava" or "minecraft:flowing_lava")
                return false;
        }

        BlockPermutation below = dimension.GetPermutation(x, y - 1, z);
        return below.Identifier == "minecraft:cactus" ||
            BlockPalette.FindType(below.Identifier)?.Tags.Contains("sand", StringComparer.Ordinal) == true;
    }

    public override bool OnPlace(Player player, int x, int y, int z, byte face, Vec3 clickPosition, ref BlockPermutation permutation) =>
        HasSupport(player.Dimension, x, y, z) == true;

    public override void OnRandomTick(Dimension dimension, BlockPermutation block, int x, int y, int z) {
        if (y >= dimension.MaximumY || dimension.GetPermutation(x, y + 1, z).Identifier is not (
            "minecraft:air" or "minecraft:cave_air" or "minecraft:void_air"))
            return;

        int age = block.State.Values.GetValueOrDefault("age") is int value ? value : 0;
        if (age < 15) {
            if (BlockPalette.Find(block.Identifier, block.State, "age", age + 1) is { } aged &&
                dimension.SetPermutation(x, y, z, aged))
                dimension.BroadcastBlockUpdate(x, y, z, aged);
            return;
        }

        int height = 1;
        while (y - height > dimension.MinimumY && dimension.GetPermutation(x, y - height, z).Identifier == block.Identifier)
            height++;
        if (height >= 3 || !dimension.SetPermutation(x, y + 1, z, Cactus) ||
            BlockPalette.Find(block.Identifier, block.State, "age", 0) is not { } reset ||
            !dimension.SetPermutation(x, y, z, reset))
            return;

        dimension.BroadcastBlockUpdate(x, y + 1, z, Cactus);
        dimension.BroadcastBlockUpdate(x, y, z, reset);
        dimension.UpdateNeighbors(x, y + 1, z);
    }

    protected override void OnSupportLost(Dimension dimension, int x, int y, int z, bool dropItem) {
        base.OnSupportLost(dimension, x, y, z, dropItem);
        dimension.UpdateNeighbors(x, y, z);
    }
}
