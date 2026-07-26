using Basalt.Core.Worlds.Dimensions.Generation.Features.Enums;

namespace Basalt.Core.Worlds.Dimensions.Generation.Features;

public static class Trees {
    private static readonly Dictionary<string, TreeFeature> Registry =
        new(StringComparer.Ordinal);

    public static readonly TreeFeature Oak;
    public static readonly TreeFeature Birch;
    public static readonly TreeFeature Spruce;
    public static readonly TreeFeature Pine;
    public static readonly TreeFeature Jungle;
    public static readonly TreeFeature MegaJungle;
    public static readonly TreeFeature MegaPine;
    public static readonly TreeFeature MegaSpruce;
    public static readonly TreeFeature Acacia;
    public static readonly TreeFeature DarkOak;
    public static readonly TreeFeature Cherry;
    public static readonly TreeFeature PaleOak;
    public static readonly TreeFeature Swamp;
    public static readonly TreeFeature FancyOak;
    public static readonly TreeFeature JungleBush;
    public static readonly HugeFungusFeature CrimsonFungus;
    public static readonly HugeFungusFeature WarpedFungus;
    public static readonly MangroveTreeFeature Mangrove;

    static Trees() {
        CrimsonFungus = new HugeFungusFeature(
            "minecraft:crimson_fungus_planted_feature",
            "minecraft:crimson_nylium",
            "minecraft:crimson_stem",
            "minecraft:nether_wart_block",
            "minecraft:shroomlight");
        WarpedFungus = new HugeFungusFeature(
            "minecraft:warped_fungus_planted_feature",
            "minecraft:warped_nylium",
            "minecraft:warped_stem",
            "minecraft:warped_wart_block",
            "minecraft:shroomlight");
        Mangrove = new MangroveTreeFeature();

        Birch = RegisterTree(
            "minecraft:birch_tree_feature",
            TreeStructure.Broadleaf,
            "minecraft:birch_log",
            "minecraft:birch_leaves");
        RegisterTree(
            "minecraft:fallen_birch_tree_feature",
            TreeStructure.Fallen,
            "minecraft:birch_log");
        RegisterTree(
            "minecraft:fallen_jungle_tree_feature",
            TreeStructure.Fallen,
            "minecraft:jungle_log");
        RegisterTree(
            "minecraft:fallen_oak_tree_feature",
            TreeStructure.Fallen,
            "minecraft:oak_log");
        RegisterTree(
            "minecraft:fallen_spruce_tree_feature",
            TreeStructure.Fallen,
            "minecraft:spruce_log");
        RegisterTree(
            "minecraft:fallen_super_birch_tree_feature",
            TreeStructure.Fallen,
            "minecraft:birch_log");
        FancyOak = RegisterTree(
            "minecraft:fancy_oak_tree_feature",
            TreeStructure.Fancy,
            "minecraft:oak_log",
            "minecraft:oak_leaves");
        JungleBush = RegisterTree(
            "minecraft:jungle_bush_feature",
            TreeStructure.Bush,
            "minecraft:jungle_log",
            "minecraft:oak_leaves");
        Jungle = RegisterTree(
            "minecraft:jungle_tree_feature",
            TreeStructure.Jungle,
            "minecraft:jungle_log",
            "minecraft:jungle_leaves",
            vines: true);
        MegaJungle = RegisterTree(
            "minecraft:mega_jungle_tree_feature",
            TreeStructure.MegaJungle,
            "minecraft:jungle_log",
            "minecraft:jungle_leaves",
            vines: true);
        MegaPine = RegisterTree(
            "minecraft:mega_pine_tree_feature",
            TreeStructure.MegaPine,
            "minecraft:spruce_log",
            "minecraft:spruce_leaves");
        MegaSpruce = RegisterTree(
            "minecraft:mega_spruce_tree_feature",
            TreeStructure.MegaSpruce,
            "minecraft:spruce_log",
            "minecraft:spruce_leaves");
        Oak = RegisterTree(
            "minecraft:oak_tree_feature",
            TreeStructure.Broadleaf,
            "minecraft:oak_log",
            "minecraft:oak_leaves");
        RegisterTree(
            "minecraft:oak_tree_with_vines_feature",
            TreeStructure.Broadleaf,
            "minecraft:oak_log",
            "minecraft:oak_leaves",
            vines: true);
        Pine = RegisterTree(
            "minecraft:pine_tree_feature",
            TreeStructure.Pine,
            "minecraft:spruce_log",
            "minecraft:spruce_leaves");
        DarkOak = RegisterTree(
            "minecraft:roofed_tree_feature",
            TreeStructure.DarkOak,
            "minecraft:dark_oak_log",
            "minecraft:dark_oak_leaves");
        RegisterTree(
            "minecraft:roofed_tree_with_vines_feature",
            TreeStructure.DarkOak,
            "minecraft:dark_oak_log",
            "minecraft:dark_oak_leaves",
            vines: true);
        Acacia = RegisterTree(
            "minecraft:savanna_tree_feature",
            TreeStructure.Acacia,
            "minecraft:acacia_log",
            "minecraft:acacia_leaves");
        Cherry = RegisterTree(
            "minecraft:cherry_tree_feature",
            TreeStructure.Cherry,
            "minecraft:cherry_log",
            "minecraft:cherry_leaves");
        PaleOak = RegisterTree(
            "minecraft:pale_oak_tree_feature",
            TreeStructure.DarkOak,
            "minecraft:pale_oak_log",
            "minecraft:pale_oak_leaves");
        Spruce = RegisterTree(
            "minecraft:spruce_tree_feature",
            TreeStructure.Spruce,
            "minecraft:spruce_log",
            "minecraft:spruce_leaves");
        RegisterTree(
            "minecraft:spruce_tree_with_vines_feature",
            TreeStructure.Spruce,
            "minecraft:spruce_log",
            "minecraft:spruce_leaves",
            vines: true);
        RegisterTree(
            "minecraft:super_birch_tree_feature",
            TreeStructure.SuperBirch,
            "minecraft:birch_log",
            "minecraft:birch_leaves");
        Swamp = RegisterTree(
            "minecraft:swamp_tree_feature",
            TreeStructure.Swamp,
            "minecraft:oak_log",
            "minecraft:oak_leaves",
            vines: true);
        RegisterTree(
            "minecraft:undecorated_jungle_tree_feature",
            TreeStructure.Jungle,
            "minecraft:jungle_log",
            "minecraft:jungle_leaves");
        RegisterTree(
            "minecraft:undecorated_jungle_tree_with_vines_feature",
            TreeStructure.Jungle,
            "minecraft:jungle_log",
            "minecraft:jungle_leaves",
            vines: true);

        RegisterFeature(
            "minecraft:birch_tree_with_beehive_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:birch_tree_feature",
            "minecraft:beehive_search_feature");
        RegisterFeature(
            "minecraft:birch_tree_with_optional_beehive_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:birch_tree_feature",
            "minecraft:optional_beehive_feature");
        RegisterFeature(
            "minecraft:fancy_oak_tree_with_beehive_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:fancy_oak_tree_feature",
            "minecraft:beehive_search_feature");
        RegisterFeature(
            "minecraft:fancy_oak_tree_with_optional_beehive_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:fancy_oak_tree_feature",
            "minecraft:optional_beehive_feature");
        RegisterFeature(
            "minecraft:jungle_tree_with_cocoa_feature",
            TreeFeatureKind.Sequence,
            "minecraft:jungle_tree_feature",
            "minecraft:optional_jungle_tree_cocoa_feature");
        RegisterFeature(
            "minecraft:noop_undecorated_jungle_tree_feature",
            TreeFeatureKind.Scatter,
            "minecraft:select_undecorated_jungle_tree_feature");
        RegisterFeature(
            "minecraft:oak_tree_with_beehive_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:oak_tree_feature",
            "minecraft:beehive_search_feature");
        RegisterFeature(
            "minecraft:oak_tree_with_optional_beehive_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:oak_tree_feature",
            "minecraft:optional_beehive_feature");
        RegisterFeature(
            "minecraft:optional_fallen_birch_tree_feature",
            TreeFeatureKind.Scatter,
            "minecraft:fallen_birch_tree_feature");
        RegisterFeature(
            "minecraft:optional_fallen_jungle_tree_feature",
            TreeFeatureKind.Scatter,
            "minecraft:fallen_jungle_tree_feature");
        RegisterFeature(
            "minecraft:optional_fallen_oak_tree_feature",
            TreeFeatureKind.Scatter,
            "minecraft:fallen_oak_tree_feature");
        RegisterFeature(
            "minecraft:optional_fallen_spruce_tree_feature",
            TreeFeatureKind.Scatter,
            "minecraft:fallen_spruce_tree_feature");
        RegisterFeature(
            "minecraft:optional_fallen_super_birch_tree_feature",
            TreeFeatureKind.Scatter,
            "minecraft:fallen_super_birch_tree_feature");
        RegisterFeature(
            "minecraft:optional_oak_tree_with_vines_feature",
            TreeFeatureKind.Scatter,
            "minecraft:oak_tree_with_vines_feature");
        RegisterFeature(
            "minecraft:optional_roofed_tree_with_vines_feature",
            TreeFeatureKind.Scatter,
            "minecraft:roofed_tree_with_vines_feature");
        RegisterFeature(
            "minecraft:optional_spruce_tree_with_vines_feature",
            TreeFeatureKind.Scatter,
            "minecraft:spruce_tree_with_vines_feature");
        RegisterFeature(
            "minecraft:optional_undecorated_jungle_tree_with_vines_feature",
            TreeFeatureKind.Scatter,
            "minecraft:undecorated_jungle_tree_with_vines_feature");
        RegisterFeature(
            "minecraft:random_oak_tree_from_sapling_feature",
            TreeFeatureKind.WeightedRandom,
            "minecraft:select_standing_oak_tree_feature",
            "minecraft:fancy_oak_tree_feature");
        RegisterFeature(
            "minecraft:random_oak_tree_with_beehive_from_sapling_feature",
            TreeFeatureKind.WeightedRandom,
            "minecraft:oak_tree_with_beehive_feature",
            "minecraft:fancy_oak_tree_with_beehive_feature");
        RegisterFeature(
            "minecraft:random_roofed_forest_feature",
            TreeFeatureKind.WeightedRandom,
            "minecraft:huge_mushroom_feature",
            "minecraft:select_roofed_tree_feature",
            "minecraft:select_birch_tree_feature",
            "minecraft:fancy_oak_tree_feature",
            "minecraft:select_oak_tree_feature");
        RegisterFeature(
            "minecraft:random_roofed_forest_feature_with_decoration_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:random_roofed_forest_feature",
            "minecraft:scatter_tall_grass_around_forest_foliage_feature");
        RegisterFeature(
            "minecraft:select_birch_tree_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:optional_fallen_birch_tree_feature",
            "minecraft:birch_tree_feature");
        RegisterFeature(
            "minecraft:select_jungle_tree_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:optional_fallen_jungle_tree_feature",
            "minecraft:jungle_tree_with_cocoa_feature",
            "minecraft:noop_undecorated_jungle_tree_feature");
        RegisterFeature(
            "minecraft:select_oak_tree_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:optional_fallen_oak_tree_feature",
            "minecraft:select_standing_oak_tree_feature");
        RegisterFeature(
            "minecraft:select_roofed_tree_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:optional_roofed_tree_with_vines_feature",
            "minecraft:roofed_tree_feature");
        RegisterFeature(
            "minecraft:select_spruce_tree_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:optional_fallen_spruce_tree_feature",
            "minecraft:select_standing_spruce_tree_feature");
        RegisterFeature(
            "minecraft:select_standing_oak_tree_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:optional_oak_tree_with_vines_feature",
            "minecraft:oak_tree_feature");
        RegisterFeature(
            "minecraft:select_standing_spruce_tree_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:optional_spruce_tree_with_vines_feature",
            "minecraft:spruce_tree_feature");
        RegisterFeature(
            "minecraft:select_super_birch_tree_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:optional_fallen_super_birch_tree_feature",
            "minecraft:super_birch_tree_with_optional_beehive_feature");
        RegisterFeature(
            "minecraft:select_undecorated_jungle_tree_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:optional_undecorated_jungle_tree_with_vines_feature",
            "minecraft:undecorated_jungle_tree_feature");
        RegisterFeature(
            "minecraft:super_birch_tree_with_beehive_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:super_birch_tree_feature",
            "minecraft:beehive_search_feature");
        RegisterFeature(
            "minecraft:super_birch_tree_with_optional_beehive_feature",
            TreeFeatureKind.Aggregate,
            "minecraft:super_birch_tree_feature",
            "minecraft:optional_beehive_feature");
    }

    public static TreeFeature? Get(string identifier) {
        return Registry.GetValueOrDefault(identifier);
    }

    public static TreeFeature Require(string identifier) {
        return Get(identifier) ??
            throw new KeyNotFoundException($"No tree feature registered as '{identifier}'.");
    }

    public static List<TreeFeature> GetAll() {
        return [.. Registry.Values];
    }

    private static TreeFeature RegisterTree(
        string identifier,
        TreeStructure structure,
        string trunk,
        string? leaves = null,
        bool vines = false) {
        TreeFeature feature = TreeFeature.Vanilla(
            identifier,
            structure,
            new TreeBlock(trunk),
            leaves is null ? null : new TreeBlock(leaves),
            vines);
        Registry.Add(identifier, feature);
        return feature;
    }

    private static void RegisterFeature(
        string identifier,
        TreeFeatureKind kind,
        params string[] features) {
        Registry.Add(identifier, new TreeFeature(identifier, kind, features));
    }
}
