namespace Basalt.Core.Blocks;

using Basalt.Core.Item;
using Basalt.Core.Item.Traits;
using Basalt.Core.Profiling;

public static class BlockDropRegistry {
    private static readonly Dictionary<string, List<BlockDrop>> Registry = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, BlockDropData> Definitions = new(StringComparer.Ordinal);

    public static void Load(Dictionary<string, BlockDropData> drops) {
        Registry.Clear();
        Definitions.Clear();
        Registry.EnsureCapacity(drops.Count);
        Definitions.EnsureCapacity(drops.Count);

        foreach ((string identifier, BlockDropData entry) in drops) {
            if (string.IsNullOrEmpty(identifier)) {
                continue;
            }

            Definitions[identifier] = entry;

            if (!entry.TryGetValue("hand", out BlockDropToolData? hand)) {
                hand = entry.Values.FirstOrDefault();
            }

            if (hand is null || !hand.TryGetValue("default", out List<BlockDropEntryData>? entries)) {
                continue;
            }

            List<BlockDrop> blockDrops = new(entries.Count);
            for (int i = 0; i < entries.Count; i++) {
                BlockDropEntryData dropEntry = entries[i];
                blockDrops.Add(new BlockDrop(dropEntry.Identifier, dropEntry.MinAmount, dropEntry.MaxAmount, dropEntry.Chance));
            }

            Registry[identifier] = blockDrops;

            BlockType? type = BlockType.Get(identifier);
            if (type is not null) {
                type.SetDrops(blockDrops);
            }
        }
    }

    public static bool TryGetDrops(string blockIdentifier, out List<BlockDrop>? drops) {
        return Registry.TryGetValue(blockIdentifier, out drops);
    }

    public static List<ItemStack> GenerateDrops(string blockIdentifier) {
        return GenerateDrops(blockIdentifier, null);
    }

    public static List<ItemStack> GenerateDrops(string blockIdentifier, ItemStack? tool) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("BlockDropRegistry.GenerateDrops") : default;
        if (Definitions.TryGetValue(blockIdentifier, out BlockDropData? definition)) {
            string toolIdentifier = tool?.Identifier ?? "hand";
            int separator = toolIdentifier.IndexOf(':');
            if (separator >= 0) {
                toolIdentifier = toolIdentifier[(separator + 1)..];
            }

            if (!definition.TryGetValue(toolIdentifier, out BlockDropToolData? toolDrops)) {
                toolDrops = definition.TryGetValue("hand", out BlockDropToolData? handDrops) ? handDrops : null;
            }

            if (toolDrops is null) {
                return [];
            }

            string variant = "default";
            ItemStackEnchantmentTrait? enchantments = tool?.GetTrait<ItemStackEnchantmentTrait>();
            if (enchantments?.GetLevel("silk_touch") > 0 && toolDrops.ContainsKey("silkTouch")) {
                variant = "silkTouch";
            }
            else {
                int fortuneLevel = Math.Clamp(enchantments?.GetLevel("fortune") ?? 0, 0, 3);
                if (fortuneLevel > 0 && toolDrops.ContainsKey($"fortune{fortuneLevel}")) {
                    variant = $"fortune{fortuneLevel}";
                }
            }

            if (!toolDrops.TryGetValue(variant, out List<BlockDropEntryData>? entries)) {
                return [];
            }

            List<ItemStack> result = new(entries.Count);
            for (int i = 0; i < entries.Count; i++) {
                BlockDropEntryData entry = entries[i];
                if (Random.Shared.NextSingle() > entry.Chance) continue;

                ItemType? itemType = ItemType.Get(entry.Identifier);
                if (itemType is null || itemType == ItemType.Air) continue;

                int amount = Random.Shared.Next(entry.MinAmount, entry.MaxAmount + 1);
                if (amount > 0) result.Add(new ItemStack(itemType, checked((ushort)amount)));
            }

            return result;
        }

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
