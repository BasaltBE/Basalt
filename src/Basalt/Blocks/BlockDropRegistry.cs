namespace Basalt.Core.Blocks;

using Basalt.Core.Item;

public static class BlockDropRegistry {
    private static readonly Dictionary<string, List<BlockDrop>> Registry = new(StringComparer.Ordinal);

    public static void Load(List<BlockDropData> drops) {
        Registry.Clear();
        Registry.EnsureCapacity(drops.Count);

        for (int i = 0; i < drops.Count; i++) {
            BlockDropData entry = drops[i];
            if (string.IsNullOrEmpty(entry.Identifier) || entry.Drops.Count == 0) {
                continue;
            }

            List<BlockDrop> blockDrops = new(entry.Drops.Count);
            for (int j = 0; j < entry.Drops.Count; j++) {
                BlockDropEntryData dropEntry = entry.Drops[j];
                blockDrops.Add(new BlockDrop(dropEntry.Identifier, dropEntry.Min, dropEntry.Max, dropEntry.Chance));
            }

            Registry[entry.Identifier] = blockDrops;

            BlockType? type = BlockType.Get(entry.Identifier);
            if (type is not null) {
                type.SetDrops(blockDrops);
            }
        }
    }

    public static bool TryGetDrops(string blockIdentifier, out List<BlockDrop>? drops) {
        return Registry.TryGetValue(blockIdentifier, out drops);
    }

    public static List<ItemStack> GenerateDrops(string blockIdentifier) {
        BlockType? type = BlockType.Get(blockIdentifier);
        if (type is not null) {
            return type.GenerateDrops();
        }

        return [];
    }
}

public sealed class BlockDrop(string identifier, int min, int max, float chance) {
    public string Identifier { get; } = identifier;
    public int Min { get; } = min;
    public int Max { get; } = max;
    public float Chance { get; } = chance;
}
