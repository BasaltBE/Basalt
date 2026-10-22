using BasaltBE.Basalt.Blocks.Traits.Types;
using BasaltBE.Basalt.Worlds.Dimensions;

namespace BasaltBE.Basalt.Blocks.Traits;

[BlockTraitAppliesTo(
    "minecraft:blue_stained_glass_pane",
    "minecraft:hard_green_stained_glass_pane",
    "minecraft:yellow_stained_glass_pane",
    "minecraft:hard_orange_stained_glass_pane",
    "minecraft:orange_stained_glass_pane",
    "minecraft:hard_black_stained_glass_pane",
    "minecraft:brown_stained_glass_pane",
    "minecraft:light_gray_stained_glass_pane",
    "minecraft:hard_glass_pane",
    "minecraft:hard_brown_stained_glass_pane",
    "minecraft:black_stained_glass_pane",
    "minecraft:light_blue_stained_glass_pane",
    "minecraft:green_stained_glass_pane",
    "minecraft:hard_yellow_stained_glass_pane",
    "minecraft:white_stained_glass_pane",
    "minecraft:hard_lime_stained_glass_pane",
    "minecraft:hard_light_gray_stained_glass_pane",
    "minecraft:magenta_stained_glass_pane",
    "minecraft:purple_stained_glass_pane",
    "minecraft:glass_pane",
    "minecraft:hard_light_blue_stained_glass_pane",
    "minecraft:hard_magenta_stained_glass_pane",
    "minecraft:cyan_stained_glass_pane",
    "minecraft:hard_gray_stained_glass_pane",
    "minecraft:gray_stained_glass_pane",
    "minecraft:hard_red_stained_glass_pane",
    "minecraft:hard_purple_stained_glass_pane",
    "minecraft:red_stained_glass_pane",
    "minecraft:pink_stained_glass_pane",
    "minecraft:hard_white_stained_glass_pane",
    "minecraft:hard_cyan_stained_glass_pane",
    "minecraft:hard_pink_stained_glass_pane",
    "minecraft:lime_stained_glass_pane",
    "minecraft:hard_blue_stained_glass_pane",
    "minecraft:iron_bars",
    "minecraft:copper_bars",
    "minecraft:exposed_copper_bars",
    "minecraft:weathered_copper_bars",
    "minecraft:oxidized_copper_bars",
    "minecraft:waxed_copper_bars",
    "minecraft:waxed_exposed_copper_bars",
    "minecraft:waxed_weathered_copper_bars",
    "minecraft:waxed_oxidized_copper_bars"
)]
public sealed class PaneConnectionTrait : BlockTrait {
    public override bool OnNeighborUpdate(Dimension dimension, int x, int y, int z) {
        BlockPermutation current = dimension.GetPermutation(x, y, z);
        byte connections = 0;
        if (BlockPalette.PaneConnects(dimension.GetPermutation(x + 1, y, z).Identifier))
            connections |= 1;
        if (BlockPalette.PaneConnects(dimension.GetPermutation(x, y, z - 1).Identifier))
            connections |= 2;
        if (BlockPalette.PaneConnects(dimension.GetPermutation(x, y, z + 1).Identifier))
            connections |= 4;
        if (BlockPalette.PaneConnects(dimension.GetPermutation(x - 1, y, z).Identifier))
            connections |= 8;

        BlockPermutation? updated = BlockPalette.FindPanePermutation(current.Identifier, connections);
        if (updated is null || updated == current)
            return false;

        dimension.SetPermutation(x, y, z, updated);
        return true;
    }
}
