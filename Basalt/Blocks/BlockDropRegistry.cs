namespace Basalt.Core.Blocks;

using Basalt.Core.Item;

/// <summary>
/// Registry for block drop tables loaded from protocol data.
/// </summary>
public static class BlockDropRegistry
{
    private static readonly Dictionary<string, List<BlockDrop>> Registry = new(StringComparer.Ordinal);

    public static void Load(List<BlockDropData> drops)
    {
        Registry.Clear();
        Registry.EnsureCapacity(drops.Count);

        for (int i = 0; i < drops.Count; i++)
        {
            BlockDropData entry = drops[i];
            if (string.IsNullOrEmpty(entry.Identifier) || entry.Drops.Count == 0)
            {
                continue;
            }

            List<BlockDrop> blockDrops = new(entry.Drops.Count);
            for (int j = 0; j < entry.Drops.Count; j++)
            {
                BlockDropEntryData dropEntry = entry.Drops[j];
                blockDrops.Add(new BlockDrop(dropEntry.Identifier, dropEntry.Min, dropEntry.Max, dropEntry.Chance));
            }

            Registry[entry.Identifier] = blockDrops;
        }
    }

    public static bool TryGetDrops(string blockIdentifier, out List<BlockDrop>? drops)
    {
        return Registry.TryGetValue(blockIdentifier, out drops);
    }

    public static List<ItemStack> GenerateDrops(string blockIdentifier)
    {
        if (!Registry.TryGetValue(blockIdentifier, out List<BlockDrop>? drops))
        {
            return [];
        }

        List<ItemStack> items = [];
        for (int i = 0; i < drops.Count; i++)
        {
            BlockDrop drop = drops[i];
            if (Random.Shared.NextDouble() > drop.Chance)
            {
                continue;
            }

            ItemType? itemType = ItemType.Get(drop.Identifier);
            if (itemType is null || itemType == ItemType.Air)
            {
                continue;
            }

            int count = Random.Shared.Next(drop.Min, drop.Max + 1);
            if (count > 0)
            {
                items.Add(new ItemStack(itemType, checked((ushort)count)));
            }
        }

        return items;
    }
}

/// <summary>
/// Represents a single drop entry for a block.
/// </summary>
public sealed class BlockDrop(string identifier, int min, int max, float chance)
{
    public string Identifier { get; } = identifier;
    public int Min { get; } = min;
    public int Max { get; } = max;
    public float Chance { get; } = chance;
}
