using System.Text.Json;
using BasaltBE.Basalt.Blocks;

namespace BasaltBE.Basalt.Worlds.Features;

internal sealed partial class TreePlacement {
    BlockPermutation At(int x, int y, int z) => placements.TryGetValue((x, y, z), out BlockPlacement block)
        ? block.Permutation : dimension.GetPermutation(x, y, z);

    bool Roll(JsonElement chance, double fallback) {
        double probability = chance.ValueKind == JsonValueKind.Number ? chance.GetDouble() / 100
            : chance.ValueKind == JsonValueKind.Object ? (double)chance.GetProperty("numerator").GetInt32() / chance.GetProperty("denominator").GetInt32()
            : fallback;
        return random.NextDouble() < probability;
    }

    void AddVines() {
        if (BlockPalette.FindDefaultPermutation("minecraft:vine") is not { } vine) return;
        trunkData.TryGetProperty("trunk_decoration", out JsonElement trunkDecoration);
        JsonElement canopyDecoration = canopyData.ValueKind == JsonValueKind.Object && canopyData.TryGetProperty("canopy_decoration", out JsonElement decoration) ? decoration : default;
        foreach (var entry in placements.ToArray()) {
            JsonElement source = entry.Value.Kind == BlockKind.Trunk ? trunkDecoration : canopyDecoration;
            if (source.ValueKind != JsonValueKind.Object || !source.TryGetProperty("decoration_block", out JsonElement id) || id.GetString() != "minecraft:vine")
                continue;
            source.TryGetProperty("decoration_chance", out JsonElement chance);
            int maximumLength = entry.Value.Kind == BlockKind.Trunk ? 1 : Math.Clamp(Number(source, "num_steps", 4), 1, 16);
            for (int direction = 0; direction < 4; direction++) {
                if (!Roll(chance, 0.25)) continue;
                (int dx, _, int dz) = BlockDirections.OffsetByDirection[BlockDirections.ByYaw[direction]];
                int mask = 1 << ((direction + 2) & 3);
                int x = entry.Key.X + dx;
                int z = entry.Key.Z + dz;
                for (int length = 0; length < maximumLength; length++) {
                    int y = entry.Key.Y - length;
                    BlockPermutation current = At(x, y, z);
                    if (current.Identifier != "minecraft:air") break;

                    BlockPermutation block = BlockPalette.Find(vine.Identifier, vine.State, "vine_direction_bits", mask) ?? vine;
                    placements[(x, y, z)] = new BlockPlacement(block, BlockKind.Leaf);
                }
            }
        }
    }

    void AddPaleMoss() {
        if (BlockPalette.FindDefaultPermutation("minecraft:pale_hanging_moss") is not { } moss) return;
        foreach (var entry in placements.ToArray()) {
            if (entry.Value.Kind != BlockKind.Leaf || random.Next(5) != 0) continue;

            int length = random.Next(1, 4);
            for (int step = 1; step <= length; step++) {
                var position = (entry.Key.X, entry.Key.Y - step, entry.Key.Z);
                if (At(position.Item1, position.Item2, position.Item3).Identifier != "minecraft:air") break;

                bool tip = step == length || At(position.Item1, position.Item2 - 1, position.Item3).Identifier != "minecraft:air";
                BlockPermutation block = BlockPalette.Find(moss.Identifier, moss.State, "tip", tip) ?? moss;
                placements[position] = new BlockPlacement(block, BlockKind.Leaf);
            }
        }
    }

    void BuildPoplar(int x, int y, int z, int height) {
        AddBase(x, y, z, 1);
        AddVerticalTrunk(x, y, z, height, 1);
        int canopyHeight = Sample(canopyData.GetProperty("height"), 5);
        int radius = 3;
        JsonElement options = canopyData.GetProperty("radius");
        int total = 0;
        foreach (JsonElement option in options.EnumerateArray()) total += option.GetProperty("weight").GetInt32();
        int choice = random.Next(total);
        foreach (JsonElement option in options.EnumerateArray()) {
            choice -= option.GetProperty("weight").GetInt32();
            if (choice < 0) {
                radius = option.GetProperty("value").GetInt32() - 1;
                break;
            }
        }
        int branches = Sample(trunkData.GetProperty("amount_of_foliage_support_branches"), 3);
        int crownStart = y + height - Sample(trunkData.GetProperty("remaining_trunk_height_above_branches"), 4);
        Span<int> directions = stackalloc int[] { 2, 3, 4, 5 };
        random.Shuffle(directions);
        for (int branch = 0; branch < Math.Min(branches, directions.Length); branch++) {
            (int dx, _, int dz) = BlockDirections.OffsetByFace[directions[branch]];
            AddTrunk(x + dx, crownStart - 1, z + dz, dx == 0 ? "z" : "x");
        }
        double holes = canopyData.GetProperty("side_hole_chance").GetDouble();
        bool alternateCorners = random.Next(2) == 0;
        for (int layer = canopyHeight - 1; layer >= -1; layer--) {
            int layerRadius = layer == canopyHeight - 1 || layer == -1 ? Math.Max(1, radius - 2)
                : layer >= canopyHeight - 3 || layer == 0 ? radius - 1 : radius;
            bool partial = layer >= canopyHeight - 2;
            AddLeafRow(x, crownStart + layer, z, layerRadius, false,
                (signedX, signedZ, dx, dz) => {
                    if (partial && (dx == layerRadius || dz == layerRadius)) return true;

                    bool shortCorner = alternateCorners ? signedX * signedZ > 0 : signedX * signedZ < 0;
                    int cut = shortCorner ? layerRadius - 1 : partial ? layerRadius + 1 : layerRadius;
                    return dx + dz > layerRadius * 2 - cut - (random.NextDouble() < holes ? 1 : 0);
                });
        }
        int supportY = crownStart + canopyHeight - 4;
        for (int offset = -radius + 4; offset <= radius - 4; offset++) {
            if (At(x + offset, supportY, z).Identifier == leaf.Identifier) AddTrunk(x + offset, supportY, z, "x");
            if (At(x, supportY, z + offset).Identifier == leaf.Identifier) AddTrunk(x, supportY, z + offset, "z");
        }
    }
}
