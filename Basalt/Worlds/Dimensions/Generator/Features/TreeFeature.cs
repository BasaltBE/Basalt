using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Worlds.Dimensions.Generation.Features.Enums;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;

namespace Basalt.Core.Worlds.Dimensions.Generation.Features;

public sealed class TreeFeature {
    private static readonly TreeBlock[] Ground = [
        "minecraft:dirt",
        "minecraft:grass_block",
        "minecraft:podzol",
        "minecraft:farmland",
        "minecraft:mycelium",
        "minecraft:moss_block",
        "minecraft:mud"
    ];

    private static readonly TreeBlock[] Replaceable = [
        "minecraft:air",
        "minecraft:oak_leaves",
        "minecraft:spruce_leaves",
        "minecraft:birch_leaves",
        "minecraft:jungle_leaves",
        "minecraft:acacia_leaves",
        "minecraft:dark_oak_leaves",
        "minecraft:cherry_leaves",
        "minecraft:pale_oak_leaves",
        "minecraft:oak_sapling",
        "minecraft:spruce_sapling",
        "minecraft:birch_sapling",
        "minecraft:jungle_sapling",
        "minecraft:acacia_sapling",
        "minecraft:dark_oak_sapling",
        "minecraft:cherry_sapling",
        "minecraft:pale_oak_sapling",
        "minecraft:azalea",
        "minecraft:flowering_azalea",
        "minecraft:azalea_leaves",
        "minecraft:azalea_leaves_flowered",
        "minecraft:tallgrass",
        "minecraft:tall_grass",
        "minecraft:large_fern",
        "minecraft:vine",
        "minecraft:snow_layer"
    ];

    private static readonly TreeBlock[] GrowThrough = [
        "minecraft:air",
        "minecraft:tallgrass",
        "minecraft:tall_grass",
        "minecraft:large_fern",
        "minecraft:vine",
        "minecraft:snow_layer"
    ];

    private readonly TreeBlock[] _baseBlocks;
    private readonly TreeBlock[] _mayGrowOn;
    private readonly TreeBlock[] _mayReplace;
    private readonly TreeBlock[] _mayGrowThrough;
    private readonly string[] _features;
    private readonly bool _vines;

    public readonly string Identifier;
    public readonly TreeFeatureKind Kind;
    public readonly TreeStructure Structure;
    public readonly TreeTrunk? Trunk;
    public readonly TreeCanopy? Canopy;

    public IReadOnlyList<TreeBlock> BaseBlocks => _baseBlocks;
    public IReadOnlyList<TreeBlock> MayGrowOn => _mayGrowOn;
    public IReadOnlyList<TreeBlock> MayReplace => _mayReplace;
    public IReadOnlyList<TreeBlock> MayGrowThrough => _mayGrowThrough;
    public IReadOnlyList<string> Features => _features;

    public TreeFeature(
        string identifier,
        TreeTrunk trunk,
        TreeCanopy canopy,
        IEnumerable<TreeBlock> baseBlocks,
        IEnumerable<TreeBlock> mayGrowOn,
        IEnumerable<TreeBlock> mayReplace,
        IEnumerable<TreeBlock>? mayGrowThrough = null) :
        this(
            identifier,
            TreeFeatureKind.Tree,
            TreeStructure.Generic,
            trunk,
            canopy,
            baseBlocks,
            mayGrowOn,
            mayReplace,
            mayGrowThrough,
            [],
            false) {
    }

    public TreeFeature(
        string identifier,
        TreeStructure structure,
        TreeTrunk trunk,
        TreeCanopy? canopy,
        IEnumerable<TreeBlock> baseBlocks,
        IEnumerable<TreeBlock> mayGrowOn,
        IEnumerable<TreeBlock> mayReplace,
        IEnumerable<TreeBlock>? mayGrowThrough = null,
        bool vines = false) :
        this(
            identifier,
            TreeFeatureKind.Tree,
            structure,
            trunk,
            canopy,
            baseBlocks,
            mayGrowOn,
            mayReplace,
            mayGrowThrough,
            [],
            vines) {
    }

    internal TreeFeature(
        string identifier,
        TreeFeatureKind kind,
        IEnumerable<string> features) :
        this(
            identifier,
            kind,
            TreeStructure.Generic,
            null,
            null,
            [],
            [],
            [],
            [],
            features,
            false) {
    }

    private TreeFeature(
        string identifier,
        TreeFeatureKind kind,
        TreeStructure structure,
        TreeTrunk? trunk,
        TreeCanopy? canopy,
        IEnumerable<TreeBlock> baseBlocks,
        IEnumerable<TreeBlock> mayGrowOn,
        IEnumerable<TreeBlock> mayReplace,
        IEnumerable<TreeBlock>? mayGrowThrough,
        IEnumerable<string> features,
        bool vines) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

        Identifier = identifier;
        Kind = kind;
        Structure = structure;
        Trunk = trunk;
        Canopy = canopy;
        _baseBlocks = [.. baseBlocks];
        _mayGrowOn = [.. mayGrowOn];
        _mayReplace = [.. mayReplace];
        _mayGrowThrough = mayGrowThrough is null ? [] : [.. mayGrowThrough];
        _features = [.. features];
        _vines = vines;

        if (kind == TreeFeatureKind.Tree) {
            ArgumentNullException.ThrowIfNull(trunk);

            if (structure != TreeStructure.Fallen) {
                ArgumentNullException.ThrowIfNull(canopy);
            }

            if (_baseBlocks.Length == 0) {
                throw new ArgumentException(
                    "A tree requires at least one base block.",
                    nameof(baseBlocks));
            }

            if (_mayGrowOn.Length == 0) {
                throw new ArgumentException(
                    "A tree requires at least one block it may grow on.",
                    nameof(mayGrowOn));
            }
        }
    }

    internal static TreeFeature Vanilla(
        string identifier,
        TreeStructure structure,
        TreeBlock trunkBlock,
        TreeBlock? leafBlock,
        bool vines) {
        (int heightBase, int heightRandomA, int heightRandomB) = structure switch {
            TreeStructure.Fancy => (3, 11, 0),
            TreeStructure.Bush => (1, 0, 0),
            TreeStructure.Jungle => (4, 8, 0),
            TreeStructure.MegaJungle => (10, 2, 19),
            TreeStructure.Pine => (6, 4, 0),
            TreeStructure.Spruce => (5, 2, 1),
            TreeStructure.MegaPine => (13, 2, 14),
            TreeStructure.MegaSpruce => (13, 2, 14),
            TreeStructure.DarkOak => (6, 2, 1),
            TreeStructure.Acacia => (5, 2, 2),
            TreeStructure.Cherry => (7, 1, 0),
            TreeStructure.Swamp => (5, 3, 0),
            TreeStructure.SuperBirch => (5, 2, 6),
            _ when identifier.Contains("birch", StringComparison.Ordinal) => (5, 2, 0),
            _ => (4, 2, 0)
        };

        (int fallenMin, int fallenMax) = identifier switch {
            "minecraft:fallen_birch_tree_feature" => (5, 8),
            "minecraft:fallen_super_birch_tree_feature" => (5, 15),
            "minecraft:fallen_jungle_tree_feature" => (4, 11),
            "minecraft:fallen_spruce_tree_feature" => (6, 10),
            _ => (4, 7)
        };

        TreeCanopy? canopy = leafBlock is null ? null : structure switch {
            TreeStructure.Fancy =>
                new TreeCanopy(4, 4, 2, 2, 1, leafBlock, new TreeChance(1, 1)),
            TreeStructure.Pine =>
                new TreeCanopy(1, 1, 1, 1, 1, leafBlock, new TreeChance(1, 1)),
            TreeStructure.Spruce =>
                new TreeCanopy(0, 2, 2, 3, 1, leafBlock, new TreeChance(1, 1)),
            TreeStructure.MegaPine or TreeStructure.MegaSpruce =>
                new TreeCanopy(0, 0, 0, 0, 1, leafBlock, new TreeChance(1, 1)),
            TreeStructure.DarkOak =>
                new TreeCanopy(0, 0, 0, 0, 1, leafBlock, new TreeChance(1, 1)),
            TreeStructure.Acacia =>
                new TreeCanopy(0, 0, 2, 2, 1, leafBlock, new TreeChance(1, 1)),
            TreeStructure.Cherry =>
                new TreeCanopy(0, 0, 4, 4, 1, leafBlock, new TreeChance(1, 1)),
            TreeStructure.Bush =>
                new TreeCanopy(1, 1, 2, 2, 1, leafBlock, new TreeChance(1, 1)),
            TreeStructure.Swamp =>
                new TreeCanopy(0, 0, 3, 3, 1, leafBlock, new TreeChance(1, 1)),
            _ => new TreeCanopy(0, 0, 2, 2, 1, leafBlock, new TreeChance(1, 1))
        };

        return new TreeFeature(
            identifier,
            TreeFeatureKind.Tree,
            structure,
            structure == TreeStructure.Fallen
                ? new TreeTrunk(fallenMin, fallenMax, trunkBlock)
                : new TreeTrunk(
                    heightBase,
                    heightRandomA,
                    heightRandomB,
                    trunkBlock),
            canopy,
            ["minecraft:dirt"],
            Ground,
            Replaceable,
            GrowThrough,
            [],
            vines);
    }

    public bool Populate(
        ChunkColumn chunk,
        int x,
        int y,
        int z,
        Random? random = null,
        bool dirty = true) {
        ArgumentNullException.ThrowIfNull(chunk);
        Random source = random ?? Random.Shared;

        if (Kind != TreeFeatureKind.Tree) {
            return PopulateReferences(
                source,
                feature => feature.Populate(chunk, x, y, z, source, dirty));
        }

        return Populate(
            chunk.Type,
            x,
            y,
            z,
            source,
            (px, py, pz) => chunk.GetPermutation(px, py, pz),
            (px, py, pz, permutation) =>
                chunk.SetPermutation(px, py, pz, permutation, dirty: dirty),
            (px, pz) => (uint)px < 16 && (uint)pz < 16);
    }

    public bool Populate(
        ChunkColumn chunk,
        BlockPos position,
        Random? random = null,
        bool dirty = true) {
        return Populate(chunk, position.X, position.Y, position.Z, random, dirty);
    }

    public bool Populate(
        Dimension dimension,
        int x,
        int y,
        int z,
        Random? random = null,
        bool broadcast = true) {
        ArgumentNullException.ThrowIfNull(dimension);
        Random source = random ?? Random.Shared;

        if (Kind != TreeFeatureKind.Tree) {
            return PopulateReferences(
                source,
                feature => feature.Populate(dimension, x, y, z, source, broadcast));
        }

        return Populate(
            dimension.Type,
            x,
            y,
            z,
            source,
            (px, py, pz) => dimension.GetPermutation(px, py, pz),
            (px, py, pz, permutation) => dimension.SetPermutation(
                px,
                py,
                pz,
                permutation,
                broadcast: broadcast),
            static (_, _) => true);
    }

    public bool Populate(
        Dimension dimension,
        BlockPos position,
        Random? random = null,
        bool broadcast = true) {
        return Populate(dimension, position.X, position.Y, position.Z, random, broadcast);
    }

    private bool PopulateReferences(Random random, Func<TreeFeature, bool> populate) {
        if (Kind == TreeFeatureKind.Scatter && random.Next(8) != 0) {
            return false;
        }

        if (Kind == TreeFeatureKind.WeightedRandom) {
            if (_features.Length == 0) {
                return false;
            }

            int start = random.Next(_features.Length);
            for (int offset = 0; offset < _features.Length; offset++) {
                string identifier = _features[(start + offset) % _features.Length];
                TreeFeature? feature = Trees.Get(identifier);
                if (feature is not null && populate(feature)) {
                    return true;
                }
            }

            return false;
        }

        for (int i = 0; i < _features.Length; i++) {
            TreeFeature? feature = Trees.Get(_features[i]);
            if (feature is not null && populate(feature)) {
                return true;
            }
        }

        return false;
    }

    private bool Populate(
        DimensionId dimensionType,
        int x,
        int y,
        int z,
        Random random,
        Func<int, int, int, BlockPermutation> get,
        Action<int, int, int, BlockPermutation> set,
        Func<int, int, bool> horizontalBounds) {
        Dictionary<(int X, int Y, int Z), TreePlacement> placements =
            BuildPlacements(x, y, z, random, get);
        if (placements.Count == 0) {
            return false;
        }

        int minY = dimensionType == DimensionId.Overworld ? -64 : 0;
        int maxY = minY + ChunkColumn.MaxSubChunks * 16 - 1;

        foreach (((int px, int py, int pz), TreePlacement placement) in placements) {
            if (py < minY || py > maxY || !horizontalBounds(px, pz)) {
                return false;
            }

            BlockPermutation existing = get(px, py, pz);
            if (placement.Kind == TreePlacementKind.Base) {
                if (!Matches(_mayGrowOn, existing)) {
                    return false;
                }
            }
            else if (placement.Kind == TreePlacementKind.Trunk) {
                if (!Matches(_mayReplace, existing) &&
                    !Matches(_mayGrowThrough, existing)) {
                    return false;
                }
            }
            else if (!Matches(_mayReplace, existing)) {
                return false;
            }
        }

        foreach (((int px, int py, int pz), TreePlacement placement) in placements) {
            set(px, py, pz, placement.Permutation);
        }

        return true;
    }

    private Dictionary<(int X, int Y, int Z), TreePlacement> BuildPlacements(
        int x,
        int y,
        int z,
        Random random,
        Func<int, int, int, BlockPermutation> get) {
        Dictionary<(int X, int Y, int Z), TreePlacement> placements = [];
        int height = Trunk!.Sample(random);

        switch (Structure) {
            case TreeStructure.Fallen:
                BuildFallen(placements, x, y, z, height, random, get);
                break;
            case TreeStructure.Fancy:
                BuildFancy(placements, x, y, z, height, random, get);
                break;
            case TreeStructure.Bush:
                BuildBush(placements, x, y, z, random);
                break;
            case TreeStructure.Jungle:
                BuildBroadleaf(placements, x, y, z, height, random);
                break;
            case TreeStructure.MegaJungle:
                BuildMegaJungle(placements, x, y, z, height, random);
                break;
            case TreeStructure.Pine:
                BuildPine(placements, x, y, z, height, random);
                break;
            case TreeStructure.Spruce:
                BuildSpruce(placements, x, y, z, height, random);
                break;
            case TreeStructure.MegaPine:
                BuildMegaPine(placements, x, y, z, height, random, false, get);
                break;
            case TreeStructure.MegaSpruce:
                BuildMegaPine(placements, x, y, z, height, random, true, get);
                break;
            case TreeStructure.DarkOak:
                BuildDarkOak(placements, x, y, z, height, random);
                break;
            case TreeStructure.Acacia:
                BuildAcacia(placements, x, y, z, height, random);
                break;
            case TreeStructure.Cherry:
                BuildCherry(placements, x, y, z, height, random);
                break;
            case TreeStructure.Swamp:
                BuildStraightBlob(placements, x, y, z, height, 3, random);
                AddVines(placements, random, get);
                break;
            case TreeStructure.SuperBirch:
            case TreeStructure.Broadleaf:
                BuildBroadleaf(placements, x, y, z, height, random);
                break;
            default:
                BuildGeneric(placements, x, y, z, height, random);
                break;
        }

        if (_vines && Structure != TreeStructure.Swamp) {
            AddVines(placements, random, get);
        }

        return placements;
    }

    private void BuildGeneric(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int x,
        int y,
        int z,
        int height,
        Random random) {
        AddBase(placements, x, y, z, 1);
        AddVerticalTrunk(placements, x, y, z, height, 1);

        int layers = Canopy!.OffsetMax - Canopy.OffsetMin + 1;
        for (int layer = 0; layer < layers; layer++) {
            int layersAbove = layers - layer - 1;
            int radius = Math.Min(
                Canopy.RadiusMax,
                Canopy.RadiusMin + layersAbove / Canopy.RadiusStep);
            TreeChance chance = Canopy.VariationChances.Length == 0
                ? new TreeChance(1, 1)
                : Canopy.VariationChances[
                    Math.Min(layer, Canopy.VariationChances.Length - 1)];
            AddLayer(
                placements,
                x,
                y + height + Canopy.OffsetMin + layer,
                z,
                radius,
                random,
                chance);
        }
    }

    private void BuildBroadleaf(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int x,
        int y,
        int z,
        int height,
        Random random) {
        BuildStraightBlob(placements, x, y, z, height, 2, random);
    }

    private void BuildStraightBlob(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int x,
        int y,
        int z,
        int height,
        int radius,
        Random random) {
        AddBase(placements, x, y, z, 1);
        AddVerticalTrunk(placements, x, y, z, height, 1);

        for (int offset = 0; offset >= -3; offset--) {
            int layerRadius = Math.Max(radius - 1 - offset / 2, 0);
            AddLeafRow(
                placements,
                x,
                y + height + offset,
                z,
                layerRadius,
                false,
                (dx, dz) =>
                    dx == layerRadius &&
                    dz == layerRadius &&
                    (random.Next(2) == 0 || offset == 0));
        }
    }

    private void BuildBush(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int x,
        int y,
        int z,
        Random random) {
        AddBase(placements, x, y, z, 1);
        AddVerticalTrunk(placements, x, y, z, 1, 1);

        for (int offset = 1; offset >= -1; offset--) {
            int radius = 1 - offset;
            AddLeafRow(
                placements,
                x,
                y + 1 + offset,
                z,
                radius,
                false,
                (dx, dz) =>
                    dx == radius &&
                    dz == radius &&
                    random.Next(2) == 0);
        }
    }

    private void BuildFancy(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int x,
        int y,
        int z,
        int treeHeight,
        Random random,
        Func<int, int, int, BlockPermutation> get) {
        AddBase(placements, x, y, z, 1);
        int height = treeHeight + 2;
        int trunkHeight = (int)Math.Floor(height * 0.618D);
        int trunkTop = y + trunkHeight;
        int relativeY = height - 5;
        List<(int X, int Y, int Z, int BranchBase)> foliage = [
            (x, y + relativeY, z, trunkTop)
        ];

        for (; relativeY >= 0; relativeY--) {
            float shape = FancyTreeShape(height, relativeY);
            if (shape < 0f) {
                continue;
            }

            double radius = shape * (random.NextSingle() + 0.328D);
            double angle = random.NextSingle() * 2f * Math.PI;
            int nodeX = x + (int)Math.Floor(radius * Math.Sin(angle) + 0.5D);
            int nodeY = y + relativeY - 1;
            int nodeZ = z + (int)Math.Floor(radius * Math.Cos(angle) + 0.5D);

            if (!FancyLimb(
                placements,
                nodeX,
                nodeY,
                nodeZ,
                nodeX,
                nodeY + 5,
                nodeZ,
                false,
                get)) {
                continue;
            }

            int dx = x - nodeX;
            int dz = z - nodeZ;
            double branchHeight =
                nodeY - Math.Sqrt(dx * dx + dz * dz) * 0.381D;
            int branchBase = branchHeight > trunkTop
                ? trunkTop
                : (int)branchHeight;

            if (!FancyLimb(
                placements,
                x,
                branchBase,
                z,
                nodeX,
                nodeY,
                nodeZ,
                false,
                get)) {
                continue;
            }

            foliage.Add((nodeX, nodeY, nodeZ, branchBase));
        }

        FancyLimb(
            placements,
            x,
            y,
            z,
            x,
            y + trunkHeight,
            z,
            true,
            get);

        for (int i = 0; i < foliage.Count; i++) {
            (int nodeX, int nodeY, int nodeZ, int branchBase) = foliage[i];
            if (branchBase - y < height * 0.2D) {
                continue;
            }

            if (branchBase != nodeY || nodeX != x || nodeZ != z) {
                FancyLimb(
                    placements,
                    x,
                    branchBase,
                    z,
                    nodeX,
                    nodeY,
                    nodeZ,
                    true,
                    get);
            }

            AddFancyFoliage(placements, nodeX, nodeY, nodeZ);
        }
    }

    private static float FancyTreeShape(int height, int y) {
        if (y < height * 0.3f) {
            return -1f;
        }

        float radius = height / 2f;
        float adjacent = radius - y;
        if (Math.Abs(adjacent) >= radius) {
            return 0f;
        }

        float distance = adjacent == 0f
            ? radius
            : MathF.Sqrt(radius * radius - adjacent * adjacent);
        return distance * 0.5f;
    }

    private bool FancyLimb(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int startX,
        int startY,
        int startZ,
        int endX,
        int endY,
        int endZ,
        bool place,
        Func<int, int, int, BlockPermutation> get) {
        if (!place &&
            startX == endX &&
            startY == endY &&
            startZ == endZ) {
            return true;
        }

        int deltaX = endX - startX;
        int deltaY = endY - startY;
        int deltaZ = endZ - startZ;
        int steps = Math.Max(
            Math.Abs(deltaX),
            Math.Max(Math.Abs(deltaY), Math.Abs(deltaZ)));
        float stepX = (float)deltaX / steps;
        float stepY = (float)deltaY / steps;
        float stepZ = (float)deltaZ / steps;

        for (int step = 0; step <= steps; step++) {
            int x = startX + (int)MathF.Floor(0.5f + step * stepX);
            int y = startY + (int)MathF.Floor(0.5f + step * stepY);
            int z = startZ + (int)MathF.Floor(0.5f + step * stepZ);

            if (!place) {
                BlockPermutation existing = get(x, y, z);
                if (!Matches(_mayReplace, existing) &&
                    !Matches(_mayGrowThrough, existing) &&
                    !existing.Type.Identifier.EndsWith("_log", StringComparison.Ordinal)) {
                    return false;
                }

                continue;
            }

            int xDifference = Math.Abs(x - startX);
            int zDifference = Math.Abs(z - startZ);
            int maxDifference = Math.Max(xDifference, zDifference);
            string axis = maxDifference == 0
                ? "y"
                : xDifference == maxDifference ? "x" : "z";
            AddTrunk(placements, x, y, z, axis);
        }

        return true;
    }

    private void AddFancyFoliage(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int centerX,
        int centerY,
        int centerZ) {
        BlockPermutation leaf = Canopy!.Block.Resolve();

        for (int offsetY = 4; offsetY >= 0; offsetY--) {
            int radius = offsetY is 4 or 0 ? 2 : 3;
            for (int offsetX = -radius; offsetX <= radius; offsetX++) {
                for (int offsetZ = -radius; offsetZ <= radius; offsetZ++) {
                    int absoluteX = Math.Abs(offsetX);
                    int absoluteZ = Math.Abs(offsetZ);
                    if ((absoluteX + 0.5f) * (absoluteX + 0.5f) +
                        (absoluteZ + 0.5f) * (absoluteZ + 0.5f) >
                        radius * radius) {
                        continue;
                    }

                    var position = (
                        centerX + offsetX,
                        centerY + offsetY,
                        centerZ + offsetZ);
                    if (placements.TryGetValue(position, out TreePlacement existing) &&
                        existing.Kind == TreePlacementKind.Trunk) {
                        continue;
                    }

                    placements[position] =
                        new TreePlacement(leaf, TreePlacementKind.Leaf);
                }
            }
        }
    }

    private void BuildMegaJungle(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int x,
        int y,
        int z,
        int height,
        Random random) {
        AddBase(placements, x, y, z, 2);
        for (int offsetY = 0; offsetY < height; offsetY++) {
            AddTrunk(placements, x, y + offsetY, z, "y");
            if (offsetY < height - 1) {
                AddTrunk(placements, x + 1, y + offsetY, z, "y");
                AddTrunk(placements, x, y + offsetY, z + 1, "y");
                AddTrunk(placements, x + 1, y + offsetY, z + 1, "y");
            }
        }

        List<FoliageAttachment> foliage = [
            new FoliageAttachment(x, y + height, z, 0, true)
        ];

        for (int branchHeight = height - 2 - random.Next(4);
            branchHeight > height / 2;
            branchHeight -= 2 + random.Next(4)) {
            float angle = random.NextSingle() * 2f * MathF.PI;
            int branchX = 0;
            int branchZ = 0;
            for (int branch = 0; branch < 5; branch++) {
                branchX = (int)(1.5f + MathF.Cos(angle) * branch);
                branchZ = (int)(1.5f + MathF.Sin(angle) * branch);
                AddTrunk(
                    placements,
                    x + branchX,
                    y + branchHeight - 3 + branch / 2,
                    z + branchZ,
                    "y");
            }

            foliage.Add(new FoliageAttachment(
                x + branchX,
                y + branchHeight,
                z + branchZ,
                -2,
                false));
        }

        for (int i = 0; i < foliage.Count; i++) {
            FoliageAttachment attachment = foliage[i];
            int foliageHeight = attachment.DoubleTrunk ? 2 : 1 + random.Next(2);
            for (int offsetY = 0; offsetY >= -foliageHeight; offsetY--) {
                int radius = 2 + attachment.RadiusOffset + 1 - offsetY;
                AddLeafRow(
                    placements,
                    attachment.X,
                    attachment.Y + offsetY,
                    attachment.Z,
                    radius,
                    attachment.DoubleTrunk,
                    (dx, dz) =>
                        dx + dz >= 7 ||
                        dx * dx + dz * dz > radius * radius);
            }
        }
    }

    private void BuildPine(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int x,
        int y,
        int z,
        int height,
        Random random) {
        int foliageHeight = random.Next(3, 5);
        int trunkHeight = height - foliageHeight;
        int leafRadius = 1 + random.Next(Math.Max(trunkHeight + 1, 1));
        const int foliageOffset = 1;

        AddBase(placements, x, y, z, 1);
        AddVerticalTrunk(placements, x, y, z, height, 1);

        int radius = 0;
        for (int offsetY = foliageOffset;
            offsetY >= foliageOffset - foliageHeight;
            offsetY--) {
            int currentRadius = radius;
            AddLeafRow(
                placements,
                x,
                y + height + offsetY,
                z,
                currentRadius,
                false,
                (dx, dz) =>
                    dx == currentRadius &&
                    dz == currentRadius &&
                    currentRadius > 0);

            if (radius >= 1 &&
                offsetY == foliageOffset - foliageHeight + 1) {
                radius--;
            }
            else if (radius < leafRadius) {
                radius++;
            }
        }
    }

    private void BuildSpruce(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int x,
        int y,
        int z,
        int height,
        Random random) {
        int foliageHeight = Math.Max(4, height - random.Next(1, 3));
        int leafRadius = random.Next(2, 4);
        int foliageOffset = random.Next(3);

        AddBase(placements, x, y, z, 1);
        AddVerticalTrunk(placements, x, y, z, height, 1);

        int radius = random.Next(2);
        int maximumRadius = 1;
        int minimumRadius = 0;
        for (int offsetY = foliageOffset; offsetY >= -foliageHeight; offsetY--) {
            int currentRadius = radius;
            AddLeafRow(
                placements,
                x,
                y + height + offsetY,
                z,
                currentRadius,
                false,
                (dx, dz) =>
                    dx == currentRadius &&
                    dz == currentRadius &&
                    currentRadius > 0);

            if (radius >= maximumRadius) {
                radius = minimumRadius;
                minimumRadius = 1;
                maximumRadius = Math.Min(maximumRadius + 1, leafRadius);
            }
            else {
                radius++;
            }
        }
    }

    private void BuildMegaPine(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int x,
        int y,
        int z,
        int height,
        Random random,
        bool spruce,
        Func<int, int, int, BlockPermutation> get) {
        int foliageHeight = spruce
            ? random.Next(13, 18)
            : random.Next(3, 8);

        AddBase(placements, x, y, z, 2);
        for (int offsetY = 0; offsetY < height; offsetY++) {
            AddTrunk(placements, x, y + offsetY, z, "y");
            if (offsetY < height - 1) {
                AddTrunk(placements, x + 1, y + offsetY, z, "y");
                AddTrunk(placements, x, y + offsetY, z + 1, "y");
                AddTrunk(placements, x + 1, y + offsetY, z + 1, "y");
            }
        }

        int previousRadius = 0;
        for (int absoluteY = Math.Max(y, y + height - foliageHeight);
            absoluteY <= y + height;
            absoluteY++) {
            int offset = y + height - absoluteY;
            int smoothRadius =
                (int)MathF.Floor((float)offset / foliageHeight * 3.5f);
            int radius = offset > 0 &&
                smoothRadius == previousRadius &&
                (absoluteY & 1) == 0
                ? smoothRadius + 1
                : smoothRadius;

            AddLeafRow(
                placements,
                x,
                absoluteY,
                z,
                radius,
                true,
                (dx, dz) =>
                    dx + dz >= 7 ||
                    dx * dx + dz * dz > radius * radius);
            previousRadius = smoothRadius;
        }

        AddPodzolCircle(placements, x - 1, y, z - 1, get);
        AddPodzolCircle(placements, x + 2, y, z - 1, get);
        AddPodzolCircle(placements, x - 1, y, z + 2, get);
        AddPodzolCircle(placements, x + 2, y, z + 2, get);
        for (int i = 0; i < 5; i++) {
            int placement = random.Next(64);
            int offsetX = placement % 8;
            int offsetZ = placement / 8;
            if (offsetX == 0 ||
                offsetX == 7 ||
                offsetZ == 0 ||
                offsetZ == 7) {
                AddPodzolCircle(
                    placements,
                    x - 3 + offsetX,
                    y,
                    z - 3 + offsetZ,
                    get);
            }
        }
    }

    private void AddPodzolCircle(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int centerX,
        int y,
        int centerZ,
        Func<int, int, int, BlockPermutation> get) {
        BlockPermutation podzol = BlockPermutation.Resolve("minecraft:podzol");
        for (int offsetX = -2; offsetX <= 2; offsetX++) {
            for (int offsetZ = -2; offsetZ <= 2; offsetZ++) {
                if (Math.Abs(offsetX) == 2 && Math.Abs(offsetZ) == 2) {
                    continue;
                }

                int blockX = centerX + offsetX;
                int blockZ = centerZ + offsetZ;
                for (int blockY = y + 2; blockY >= y - 3; blockY--) {
                    BlockPermutation existing = get(blockX, blockY, blockZ);
                    if (Matches(_mayGrowOn, existing)) {
                        placements[(blockX, blockY, blockZ)] =
                            new TreePlacement(podzol, TreePlacementKind.Base);
                        break;
                    }

                    if (existing.Type.Identifier != "minecraft:air" &&
                        blockY < y) {
                        break;
                    }
                }
            }
        }
    }

    private void BuildDarkOak(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int x,
        int y,
        int z,
        int height,
        Random random) {
        AddBase(placements, x, y, z, 2);
        (int directionX, int directionZ) = HorizontalDirection(random);
        int leanHeight = height - random.Next(4);
        int leanSteps = 2 - random.Next(3);
        int trunkX = x;
        int trunkZ = z;
        int attachmentY = y + height - 1;

        for (int offsetY = 0; offsetY < height; offsetY++) {
            if (offsetY >= leanHeight && leanSteps > 0) {
                trunkX += directionX;
                trunkZ += directionZ;
                leanSteps--;
            }

            AddTrunk(placements, trunkX, y + offsetY, trunkZ, "y");
            AddTrunk(placements, trunkX + 1, y + offsetY, trunkZ, "y");
            AddTrunk(placements, trunkX, y + offsetY, trunkZ + 1, "y");
            AddTrunk(placements, trunkX + 1, y + offsetY, trunkZ + 1, "y");
        }

        List<FoliageAttachment> foliage = [
            new FoliageAttachment(trunkX, attachmentY, trunkZ, 0, true)
        ];

        for (int offsetX = -1; offsetX <= 2; offsetX++) {
            for (int offsetZ = -1; offsetZ <= 2; offsetZ++) {
                bool outsideTrunk =
                    offsetX < 0 || offsetX > 1 ||
                    offsetZ < 0 || offsetZ > 1;
                if (!outsideTrunk || random.Next(3) > 0) {
                    continue;
                }

                int length = random.Next(3) + 2;
                for (int branchY = 0; branchY < length; branchY++) {
                    AddTrunk(
                        placements,
                        x + offsetX,
                        attachmentY - branchY - 1,
                        z + offsetZ,
                        "y");
                }

                foliage.Add(new FoliageAttachment(
                    x + offsetX,
                    attachmentY,
                    z + offsetZ,
                    0,
                    false));
            }
        }

        for (int i = 0; i < foliage.Count; i++) {
            FoliageAttachment attachment = foliage[i];
            if (attachment.DoubleTrunk) {
                AddDarkOakLeafRow(
                    placements,
                    attachment,
                    -1,
                    2);
                AddDarkOakLeafRow(
                    placements,
                    attachment,
                    0,
                    3);
                AddDarkOakLeafRow(
                    placements,
                    attachment,
                    1,
                    2);
                if (random.Next(2) == 0) {
                    AddDarkOakLeafRow(
                        placements,
                        attachment,
                        2,
                        0);
                }
            }
            else {
                AddDarkOakLeafRow(
                    placements,
                    attachment,
                    -1,
                    2);
                AddDarkOakLeafRow(
                    placements,
                    attachment,
                    0,
                    1);
            }
        }
    }

    private void AddDarkOakLeafRow(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        FoliageAttachment attachment,
        int offsetY,
        int radius) {
        AddLeafRowSigned(
            placements,
            attachment.X,
            attachment.Y + offsetY,
            attachment.Z,
            radius,
            attachment.DoubleTrunk,
            (signedX, signedZ, dx, dz) => {
                if (offsetY == 0 &&
                    attachment.DoubleTrunk &&
                    (signedX == -radius || signedX >= radius) &&
                    (signedZ == -radius || signedZ >= radius)) {
                    return true;
                }

                if (offsetY == -1 && !attachment.DoubleTrunk) {
                    return dx == radius && dz == radius;
                }

                return offsetY == 1 && dx + dz > radius * 2 - 2;
            });
    }

    private void BuildAcacia(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int x,
        int y,
        int z,
        int height,
        Random random) {
        AddBase(placements, x, y, z, 1);
        List<FoliageAttachment> foliage = [];
        (int leanX, int leanZ) = HorizontalDirection(random);
        int leanHeight = height - random.Next(4) - 1;
        int leanSteps = 3 - random.Next(3);
        int trunkX = x;
        int trunkZ = z;
        int topY = y;

        for (int offsetY = 0; offsetY < height; offsetY++) {
            int blockY = y + offsetY;
            if (offsetY >= leanHeight && leanSteps > 0) {
                trunkX += leanX;
                trunkZ += leanZ;
                leanSteps--;
            }

            AddTrunk(placements, trunkX, blockY, trunkZ, "y");
            topY = blockY + 1;
        }

        foliage.Add(new FoliageAttachment(trunkX, topY, trunkZ, 1, false));

        trunkX = x;
        trunkZ = z;
        (int branchX, int branchZ) = HorizontalDirection(random);
        if (branchX != leanX || branchZ != leanZ) {
            int branchStart = leanHeight - random.Next(2) - 1;
            int branchSteps = 1 + random.Next(3);
            int branchTop = int.MinValue;
            for (int offsetY = branchStart;
                offsetY < height && branchSteps > 0;
                offsetY++, branchSteps--) {
                if (offsetY < 1) {
                    continue;
                }

                trunkX += branchX;
                trunkZ += branchZ;
                int blockY = y + offsetY;
                AddTrunk(
                    placements,
                    trunkX,
                    blockY,
                    trunkZ,
                    "y");
                branchTop = blockY + 1;
            }

            if (branchTop != int.MinValue) {
                foliage.Add(new FoliageAttachment(
                    trunkX,
                    branchTop,
                    trunkZ,
                    0,
                    false));
            }
        }

        for (int i = 0; i < foliage.Count; i++) {
            FoliageAttachment attachment = foliage[i];
            int lowerRadius = 2 + attachment.RadiusOffset;
            AddLeafRow(
                placements,
                attachment.X,
                attachment.Y - 1,
                attachment.Z,
                lowerRadius,
                false,
                (dx, dz) =>
                    dx == lowerRadius &&
                    dz == lowerRadius &&
                    lowerRadius > 0);
            AddLeafRow(
                placements,
                attachment.X,
                attachment.Y,
                attachment.Z,
                1,
                false,
                (dx, dz) =>
                    (dx > 1 || dz > 1) &&
                    dx != 0 &&
                    dz != 0);
            int upperRadius = 1 + attachment.RadiusOffset;
            AddLeafRow(
                placements,
                attachment.X,
                attachment.Y,
                attachment.Z,
                upperRadius,
                false,
                (dx, dz) =>
                    (dx > 1 || dz > 1) &&
                    dx != 0 &&
                    dz != 0);
        }
    }

    private void BuildFallen(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int x,
        int y,
        int z,
        int length,
        Random random,
        Func<int, int, int, BlockPermutation> get) {
        AddTrunk(placements, x, y, z, "y");

        (int directionX, int directionZ) = HorizontalDirection(random);
        int logLength = length - 2;
        int gap = 2 + random.Next(2);
        int startX = x + directionX * gap;
        int startY = y + 1;
        int startZ = z + directionZ * gap;
        for (int fall = 0; fall < 6; fall++) {
            BlockPermutation current = get(startX, startY, startZ);
            BlockPermutation below = get(startX, startY - 1, startZ);
            if ((Matches(_mayReplace, current) ||
                Matches(_mayGrowThrough, current)) &&
                below.Type.Identifier != "minecraft:air") {
                break;
            }

            startY--;
        }

        int gapInGround = 0;
        for (int step = 0; step < logLength; step++) {
            int blockX = startX + directionX * step;
            int blockZ = startZ + directionZ * step;
            BlockPermutation existing = get(blockX, startY, blockZ);
            if (!Matches(_mayReplace, existing) &&
                !Matches(_mayGrowThrough, existing)) {
                return;
            }

            if (get(blockX, startY - 1, blockZ).Type.Solid) {
                gapInGround = 0;
            }
            else if (++gapInGround > 2) {
                return;
            }
        }

        for (int step = 0; step < logLength; step++) {
            AddTrunk(
                placements,
                startX + directionX * step,
                startY,
                startZ + directionZ * step,
                directionX == 0 ? "z" : "x");
        }
    }

    private void BuildCherry(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int x,
        int y,
        int z,
        int height,
        Random random) {
        AddBase(placements, x, y, z, 1);
        int firstBranchOffset = Math.Max(
            0,
            height - 1 + random.Next(-4, -2));
        int secondBranchOffset = Math.Max(0, height - 5);
        if (secondBranchOffset >= firstBranchOffset) {
            secondBranchOffset++;
        }

        int branchCount = random.Next(1, 4);
        bool middleBranch = branchCount == 3;
        bool sideBranches = branchCount >= 2;
        int trunkHeight = middleBranch
            ? height
            : sideBranches
                ? Math.Max(firstBranchOffset, secondBranchOffset) + 1
                : firstBranchOffset + 1;
        AddVerticalTrunk(placements, x, y, z, trunkHeight, 1);

        List<FoliageAttachment> foliage = [];
        if (middleBranch) {
            foliage.Add(new FoliageAttachment(
                x,
                y + trunkHeight,
                z,
                0,
                false));
        }

        (int directionX, int directionZ) = HorizontalDirection(random);
        foliage.Add(BuildCherryBranch(
            placements,
            x,
            y,
            z,
            height,
            firstBranchOffset,
            firstBranchOffset < trunkHeight - 1,
            directionX,
            directionZ,
            random));
        if (sideBranches) {
            foliage.Add(BuildCherryBranch(
                placements,
                x,
                y,
                z,
                height,
                secondBranchOffset,
                secondBranchOffset < trunkHeight - 1,
                -directionX,
                -directionZ,
                random));
        }

        for (int i = 0; i < foliage.Count; i++) {
            AddCherryFoliage(placements, foliage[i], random);
        }
    }

    private FoliageAttachment BuildCherryBranch(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int originX,
        int originY,
        int originZ,
        int height,
        int branchOffset,
        bool trunkContinues,
        int directionX,
        int directionZ,
        Random random) {
        int branchX = originX;
        int branchY = originY + branchOffset;
        int branchZ = originZ;
        int endOffset = height - 1 + random.Next(-1, 1);
        bool extends = trunkContinues || endOffset < branchOffset;
        int distance = random.Next(2, 5) + (extends ? 1 : 0);
        int endX = originX + directionX * distance;
        int endY = originY + endOffset;
        int endZ = originZ + directionZ * distance;
        string horizontalAxis = directionX == 0 ? "z" : "x";

        int horizontalSteps = extends ? 2 : 1;
        for (int step = 0; step < horizontalSteps; step++) {
            branchX += directionX;
            branchZ += directionZ;
            AddTrunk(
                placements,
                branchX,
                branchY,
                branchZ,
                horizontalAxis);
        }

        while (branchX != endX || branchY != endY || branchZ != endZ) {
            int remaining =
                Math.Abs(endX - branchX) +
                Math.Abs(endY - branchY) +
                Math.Abs(endZ - branchZ);
            float verticalChance =
                (float)Math.Abs(endY - branchY) / remaining;
            if (random.NextSingle() < verticalChance) {
                branchY += Math.Sign(endY - branchY);
                AddTrunk(placements, branchX, branchY, branchZ, "y");
            }
            else {
                branchX += directionX;
                branchZ += directionZ;
                AddTrunk(
                    placements,
                    branchX,
                    branchY,
                    branchZ,
                    horizontalAxis);
            }
        }

        return new FoliageAttachment(
            endX,
            endY + 1,
            endZ,
            0,
            false);
    }

    private void AddCherryFoliage(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        FoliageAttachment attachment,
        Random random) {
        AddCherryLeafRow(placements, attachment, 2, 1, random);
        AddCherryLeafRow(placements, attachment, 1, 2, random);
        AddCherryLeafRow(placements, attachment, 0, 3, random);
        AddCherryLeafRow(placements, attachment, -1, 3, random);
        AddCherryHangingLeaves(
            placements,
            attachment,
            -1,
            3,
            random,
            1f / 6f,
            1f / 3f);
        AddCherryLeafRow(placements, attachment, -2, 2, random);
        AddCherryHangingLeaves(
            placements,
            attachment,
            -2,
            2,
            random,
            1f / 6f,
            1f / 3f);
    }

    private void AddCherryLeafRow(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        FoliageAttachment attachment,
        int offsetY,
        int radius,
        Random random) {
        AddLeafRow(
            placements,
            attachment.X,
            attachment.Y + offsetY,
            attachment.Z,
            radius,
            false,
            (dx, dz) => {
                if (offsetY == -1 &&
                    (dx == radius || dz == radius) &&
                    random.NextSingle() < 0.25f) {
                    return true;
                }

                bool corner = dx == radius && dz == radius;
                if (radius > 2) {
                    return corner ||
                        dx + dz > radius * 2 - 2 &&
                        random.NextSingle() < 0.5f;
                }

                return corner && random.NextSingle() < 0.5f;
            });
    }

    private void AddCherryHangingLeaves(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        FoliageAttachment attachment,
        int rowOffset,
        int radius,
        Random random,
        float chance,
        float extensionChance) {
        BlockPermutation leaf = Canopy!.Block.Resolve();
        HashSet<(int X, int Z)> edge = [];
        for (int offset = -radius; offset < radius; offset++) {
            edge.Add((-radius, offset));
            edge.Add((radius, offset));
            edge.Add((offset, -radius));
            edge.Add((offset, radius));
        }

        foreach ((int offsetX, int offsetZ) in edge) {
            var above = (
                attachment.X + offsetX,
                attachment.Y + rowOffset,
                attachment.Z + offsetZ);
            if (!placements.TryGetValue(above, out TreePlacement existing) ||
                existing.Kind != TreePlacementKind.Leaf ||
                random.NextSingle() > chance) {
                continue;
            }

            var hanging = (above.Item1, above.Item2 - 1, above.Item3);
            int distance =
                Math.Abs(hanging.Item1 - attachment.X) +
                Math.Abs(hanging.Item2 - (attachment.Y - 1)) +
                Math.Abs(hanging.Item3 - attachment.Z);
            if (distance >= 7 || placements.ContainsKey(hanging)) {
                continue;
            }

            placements[hanging] =
                new TreePlacement(leaf, TreePlacementKind.Leaf);
            var extension = (
                hanging.Item1,
                hanging.Item2 - 1,
                hanging.Item3);
            if (random.NextSingle() <= extensionChance &&
                !placements.ContainsKey(extension)) {
                placements[extension] =
                    new TreePlacement(leaf, TreePlacementKind.Leaf);
            }
        }
    }

    private void AddBase(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int x,
        int y,
        int z,
        int width) {
        for (int ox = 0; ox < width; ox++) {
            for (int oz = 0; oz < width; oz++) {
                TreeBlock block = _baseBlocks[(ox + oz) % _baseBlocks.Length];
                placements[(x + ox, y - 1, z + oz)] =
                    new TreePlacement(block.Resolve(), TreePlacementKind.Base);
            }
        }
    }

    private void AddVerticalTrunk(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int x,
        int y,
        int z,
        int height,
        int width) {
        for (int py = y; py < y + height; py++) {
            for (int ox = 0; ox < width; ox++) {
                for (int oz = 0; oz < width; oz++) {
                    AddTrunk(placements, x + ox, py, z + oz, "y");
                }
            }
        }
    }

    private void AddTrunk(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int x,
        int y,
        int z,
        string axis) {
        BlockPermutation permutation = Trunk!.Block.Resolve();
        if (permutation.Type.States.Contains("pillar_axis")) {
            permutation = BlockPermutation.Resolve(
                permutation.Type.Identifier,
                new BlockState { ["pillar_axis"] = axis });
        }

        placements[(x, y, z)] =
            new TreePlacement(permutation, TreePlacementKind.Trunk);
    }

    private void AddLayer(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int centerX,
        int y,
        int centerZ,
        int radius,
        Random random,
        TreeChance cornerChance) {
        if (Canopy is null) {
            return;
        }

        BlockPermutation leaf = Canopy.Block.Resolve();
        for (int x = centerX - radius; x <= centerX + radius; x++) {
            for (int z = centerZ - radius; z <= centerZ + radius; z++) {
                bool corner = radius > 0 &&
                    Math.Abs(x - centerX) == radius &&
                    Math.Abs(z - centerZ) == radius;
                if (corner && !cornerChance.Roll(random)) {
                    continue;
                }

                if (placements.TryGetValue((x, y, z), out TreePlacement existing) &&
                    existing.Kind == TreePlacementKind.Trunk) {
                    continue;
                }

                placements[(x, y, z)] =
                    new TreePlacement(leaf, TreePlacementKind.Leaf);
            }
        }
    }

    private void AddLeafRow(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int centerX,
        int y,
        int centerZ,
        int radius,
        bool doubleTrunk,
        Func<int, int, bool> skip) {
        AddLeafRowSigned(
            placements,
            centerX,
            y,
            centerZ,
            radius,
            doubleTrunk,
            (_, _, distanceX, distanceZ) => skip(distanceX, distanceZ));
    }

    private void AddLeafRowSigned(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        int centerX,
        int y,
        int centerZ,
        int radius,
        bool doubleTrunk,
        Func<int, int, int, int, bool> skip) {
        if (radius < 0) {
            return;
        }

        BlockPermutation leaf = Canopy!.Block.Resolve();
        int extra = doubleTrunk ? 1 : 0;
        for (int offsetX = -radius; offsetX <= radius + extra; offsetX++) {
            for (int offsetZ = -radius; offsetZ <= radius + extra; offsetZ++) {
                int distanceX = doubleTrunk
                    ? Math.Min(Math.Abs(offsetX), Math.Abs(offsetX - 1))
                    : Math.Abs(offsetX);
                int distanceZ = doubleTrunk
                    ? Math.Min(Math.Abs(offsetZ), Math.Abs(offsetZ - 1))
                    : Math.Abs(offsetZ);
                if (skip(offsetX, offsetZ, distanceX, distanceZ)) {
                    continue;
                }

                var position = (centerX + offsetX, y, centerZ + offsetZ);
                if (placements.TryGetValue(position, out TreePlacement existing) &&
                    existing.Kind == TreePlacementKind.Trunk) {
                    continue;
                }

                placements[position] =
                    new TreePlacement(leaf, TreePlacementKind.Leaf);
            }
        }
    }

    private void AddVines(
        Dictionary<(int X, int Y, int Z), TreePlacement> placements,
        Random random,
        Func<int, int, int, BlockPermutation> get) {
        BlockPermutation vine = BlockPermutation.Resolve("minecraft:vine");
        List<(int X, int Y, int Z)> logs = [];
        List<(int X, int Y, int Z)> leaves = [];
        foreach (((int x, int y, int z), TreePlacement placement) in placements) {
            if (placement.Kind == TreePlacementKind.Trunk) {
                logs.Add((x, y, z));
            }
            else if (placement.Kind == TreePlacementKind.Leaf) {
                leaves.Add((x, y, z));
            }
        }

        (int X, int Z)[] directions = [
            (-1, 0),
            (1, 0),
            (0, -1),
            (0, 1)
        ];

        for (int i = 0; i < logs.Count; i++) {
            (int x, int y, int z) = logs[i];
            for (int direction = 0; direction < directions.Length; direction++) {
                if (random.Next(3) == 0) {
                    continue;
                }

                (int dx, int dz) = directions[direction];
                var position = (x + dx, y, z + dz);
                if (!placements.ContainsKey(position) &&
                    Matches(_mayReplace, get(position.Item1, position.Item2, position.Item3))) {
                    placements[position] =
                        new TreePlacement(vine, TreePlacementKind.Leaf);
                }
            }
        }

        for (int i = 0; i < leaves.Count; i++) {
            (int x, int y, int z) = leaves[i];
            for (int direction = 0; direction < directions.Length; direction++) {
                if (random.NextSingle() >= 0.25f) {
                    continue;
                }

                (int dx, int dz) = directions[direction];
                int vineX = x + dx;
                int vineY = y;
                int vineZ = z + dz;
                for (int length = 0; length <= 4; length++, vineY--) {
                    var position = (vineX, vineY, vineZ);
                    if (placements.ContainsKey(position) ||
                        !Matches(_mayReplace, get(vineX, vineY, vineZ))) {
                        break;
                    }

                    placements[position] =
                        new TreePlacement(vine, TreePlacementKind.Leaf);
                }
            }
        }
    }

    private static bool Matches(TreeBlock[] blocks, BlockPermutation permutation) {
        for (int i = 0; i < blocks.Length; i++) {
            if (blocks[i].Matches(permutation)) {
                return true;
            }
        }

        return false;
    }

    private static (int X, int Z) HorizontalDirection(Random random) {
        return random.Next(4) switch {
            0 => (1, 0),
            1 => (-1, 0),
            2 => (0, 1),
            _ => (0, -1)
        };
    }

    private readonly record struct TreePlacement(
        BlockPermutation Permutation,
        TreePlacementKind Kind);

    private readonly record struct FoliageAttachment(
        int X,
        int Y,
        int Z,
        int RadiusOffset,
        bool DoubleTrunk);

    private enum TreePlacementKind : byte {
        Base,
        Leaf,
        Trunk
    }
}
