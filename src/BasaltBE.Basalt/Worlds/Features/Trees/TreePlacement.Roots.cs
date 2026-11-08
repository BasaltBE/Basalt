using System.Text.Json;
using BasaltBE.Basalt.Blocks;

namespace BasaltBE.Basalt.Worlds.Features;

internal sealed partial class TreePlacement {
    void BuildAzalea(int x, int y, int z, int height) {
        AddBase(x, y, z, 1);
        JsonElement lean = trunkData.GetProperty("trunk_lean");
        int leanStart = height - Sample(lean.GetProperty("lean_height"), 2);
        int leanSteps = Sample(lean.GetProperty("lean_steps"), 3);
        (int dx, int dz) = HorizontalDirection();
        int trunkX = x;
        int trunkZ = z;
        for (int offset = 0; offset < height; offset++) {
            if (offset >= leanStart && leanSteps-- > 0) {
                trunkX += dx;
                trunkZ += dz;
            }
            AddTrunk(trunkX, y + offset, trunkZ, "y");
        }
        SpreadLeaves(trunkX, y + height, trunkZ);
    }

    void SpreadLeaves(int x, int y, int z) {
        int radius = Math.Clamp(Number(canopyData, "canopy_radius", 3), 1, 16);
        int height = Math.Clamp(Number(canopyData, "canopy_height", 2), 1, 16);
        int attempts = Math.Clamp(Number(canopyData, "leaf_placement_attempts", 50), 1, 4096);
        for (int index = 0; index < attempts; index++) {
            var position = (x + random.Next(radius) - random.Next(radius),
                y + random.Next(height) - random.Next(height),
                z + random.Next(radius) - random.Next(radius));
            if (!placements.ContainsKey(position)) placements[position] = new BlockPlacement(SelectLeaf(), BlockKind.Leaf);
        }
    }

    void BuildMangrove(int x, int y, int z, int height) {
        JsonElement roots = definition.GetProperty("mangrove_roots");
        int trunkY = y + Sample(roots.GetProperty("y_offset"), 3);
        BlockPermutation root = Resolve(roots.GetProperty("root_block"))!;
        BlockPermutation muddyRoot = Resolve(roots.GetProperty("muddy_root_block"))!;
        JsonElement aboveRoot = roots.GetProperty("above_root");
        BlockPermutation? rootDecoration = Resolve(aboveRoot.GetProperty("above_root_block"));
        HashSet<(int X, int Y, int Z)> rootPositions = [];
        for (int rootY = y; rootY < trunkY; rootY++)
            rootPositions.Add((x, rootY, z));
        for (int face = 2; face < 6; face++) {
            (int dx, _, int dz) = BlockDirections.OffsetByFace[face];
            var start = (x + dx, trunkY, z + dz);
            rootPositions.Add(start);
            GrowRoots(start, dx, dz, x, trunkY, z, roots, rootPositions, 0);
        }
        foreach ((int rootX, int rootY, int rootZ) in rootPositions) {
            BlockPermutation current = dimension.GetPermutation(rootX, rootY, rootZ);
            if (!CanRootThrough(current)) continue;
            placements[(rootX, rootY, rootZ)] = new BlockPlacement(
                current.Identifier is "minecraft:mud" or "minecraft:muddy_mangrove_roots" ? muddyRoot : root,
                BlockKind.Root);
            if (rootDecoration is not null && At(rootX, rootY + 1, rootZ).Identifier == "minecraft:air" && Roll(aboveRoot.GetProperty("above_root_chance"), 0.5))
                placements[(rootX, rootY + 1, rootZ)] = new BlockPlacement(rootDecoration, BlockKind.Leaf);
        }

        List<(int X, int Y, int Z)> foliage = [];
        JsonElement branches = trunkData.GetProperty("branches");
        for (int offset = 0; offset < height; offset++) {
            AddTrunk(x, trunkY + offset, z, "y");
            if (offset == height - 1) {
                foliage.Add((x, trunkY + height, z));
                continue;
            }
            if (random.Next(2) != 0) continue;
            (int dx, int dz) = HorizontalDirection();
            int steps = Sample(branches.GetProperty("branch_steps"), 3);
            int start = Sample(branches.GetProperty("branch_length"), 1);
            int branchX = x;
            int branchZ = z;
            for (int step = 1; step <= steps; step++) {
                branchX += dx;
                branchZ += dz;
                int branchY = trunkY + offset + start + step;
                AddTrunk(branchX, branchY, branchZ, dx == 0 ? "z" : "x");
                foliage.Add((branchX, branchY + 1, branchZ));
            }
        }
        foreach ((int leafX, int leafY, int leafZ) in foliage) SpreadLeaves(leafX, leafY, leafZ);

        if (canopyData.TryGetProperty("hanging_block", out JsonElement hanging) && Resolve(hanging) is { } propagule) {
            foreach (var entry in placements.ToArray()) {
                if (entry.Value.Kind != BlockKind.Leaf || !Roll(canopyData.GetProperty("hanging_block_placement_chance"), 0.25))
                    continue;
                var position = (entry.Key.X, entry.Key.Y - 1, entry.Key.Z);
                if (At(position.Item1, position.Item2, position.Item3).Identifier == "minecraft:air")
                    placements[position] = new BlockPlacement(BlockPalette.Find(propagule.Identifier, propagule.State, "propagule_stage", random.Next(5)) ?? propagule, BlockKind.Leaf);
            }
        }
    }

    void GrowRoots((int X, int Y, int Z) position, int dx, int dz, int originX, int originY, int originZ,
        JsonElement roots, HashSet<(int X, int Y, int Z)> positions, int depth) {
        int maximumLength = Math.Clamp(Number(roots, "max_root_length", 15), 1, 32);
        if (depth >= maximumLength || position.Y <= dimension.MinimumY) return;

        int width = Math.Abs(position.X - originX) + Math.Abs(position.Y - originY) + Math.Abs(position.Z - originZ);
        int maximumWidth = Math.Clamp(Number(roots, "max_root_width", 8), 1, 16);
        var next = width > maximumWidth || random.NextSingle() < 0.2f || random.Next(2) == 0
            ? (position.X, position.Y - 1, position.Z)
            : (position.X + dx, position.Y, position.Z + dz);
        if (!CanRootThrough(dimension.GetPermutation(next.Item1, next.Item2, next.Item3)) || !positions.Add(next)) return;
        GrowRoots(next, dx, dz, originX, originY, originZ, roots, positions, depth + 1);
    }
}
