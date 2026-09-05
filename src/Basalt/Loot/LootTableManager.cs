namespace Basalt.Core.Loot;

using System.Text.Json;

using Basalt.Core.Blocks;
using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Item;
using Basalt.Core.Item.Traits;

public static class LootTableManager {
    private static readonly Dictionary<string, LootTable> Tables = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, LootTable> EntityTables = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, JsonElement> Definitions = new(StringComparer.Ordinal);

    public static void LoadFromEntities(string dataRoot, IEnumerable<EntityType> entityTypes) {
        using FileStream stream = File.OpenRead(Path.Combine(dataRoot, "entity_drops.json"));
        LoadFromEntities(stream, entityTypes);
    }

    public static void LoadFromEntities(Stream stream, IEnumerable<EntityType> entityTypes) {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(entityTypes);

        Tables.Clear();
        EntityTables.Clear();
        Definitions.Clear();

        using JsonDocument document = JsonDocument.Parse(stream);
        if (document.RootElement.ValueKind != JsonValueKind.Object) {
            throw new JsonException("Entity drops data must contain an object of named loot tables.");
        }

        foreach (JsonProperty property in document.RootElement.EnumerateObject()) {
            Definitions[NormalizePath(property.Name)] = property.Value.Clone();
        }

        foreach (EntityType entityType in entityTypes) {
            if (string.IsNullOrWhiteSpace(entityType.LootTablePath)) {
                continue;
            }

            LootTable table = LoadTable(entityType.LootTablePath);
            EntityTables[entityType.Identifier] = table;
        }
    }

    public static List<ItemStack> GenerateLootFromEntity(Basalt.Core.Entities.Entity entity, Basalt.Core.Entities.Entity? killer = null) {
        ItemStack? weapon = killer?.GetTrait<EntityInventoryTrait>()?.GetHeldItem();
        return GenerateLootFromEntityType(entity.Type, weapon);
    }

    public static List<ItemStack> GenerateLootFromEntityType(EntityType entityType) {
        return GenerateLootFromEntityType(entityType, null);
    }

    private static List<ItemStack> GenerateLootFromEntityType(EntityType entityType, ItemStack? weapon) {
        if (Definitions.TryGetValue(entityType.Identifier, out JsonElement definition) &&
            definition.ValueKind == JsonValueKind.Object &&
            !definition.TryGetProperty("pools", out _)) {
            return GenerateEntityDrops(definition, weapon);
        }

        if (!EntityTables.TryGetValue(entityType.Identifier, out LootTable? table)) {
            return [];
        }

        return table.Generate();
    }

    private static List<ItemStack> GenerateEntityDrops(JsonElement definition, ItemStack? weapon) {
        JsonElement variants = definition;
        if (!definition.TryGetProperty("default", out JsonElement defaultEntries) ||
            defaultEntries.ValueKind != JsonValueKind.Array) {
            string toolIdentifier = weapon?.Identifier ?? string.Empty;
            int separator = toolIdentifier.IndexOf(':');
            if (separator >= 0) {
                toolIdentifier = toolIdentifier[(separator + 1)..];
            }

            if (!definition.TryGetProperty(toolIdentifier, out variants) ||
                variants.ValueKind != JsonValueKind.Object) {
                using JsonElement.ObjectEnumerator tools = definition.EnumerateObject();
                if (!tools.MoveNext()) {
                    return [];
                }

                variants = tools.Current.Value;
            }
        }

        if (variants.ValueKind != JsonValueKind.Object) {
            return [];
        }

        ItemStackEnchantmentTrait? enchantments = weapon?.GetTrait<ItemStackEnchantmentTrait>();
        int looting = Math.Clamp(enchantments?.GetLevel("looting") ?? 0, 0, 3);
        int fireAspect = Math.Clamp(enchantments?.GetLevel("fire_aspect") ?? 0, 0, 2);
        string variant = fireAspect > 0 && looting > 0
            ? $"looting{looting}_fireAspect{fireAspect}"
            : fireAspect > 0 ? $"fireAspect{fireAspect}"
            : looting > 0 ? $"looting{looting}" : "default";

        if (!variants.TryGetProperty(variant, out JsonElement entries) ||
            entries.ValueKind != JsonValueKind.Array) {
            return [];
        }

        List<ItemStack> drops = new(entries.GetArrayLength());
        foreach (JsonElement entry in entries.EnumerateArray()) {
            string identifier = ReadString(entry, "identifier");
            ItemType? itemType = ItemType.Get(identifier);
            if (itemType is null || itemType == ItemType.Air) {
                continue;
            }

            int minAmount = Math.Max(0, ReadInt(entry, "minAmount", 0));
            int maxAmount = Math.Max(minAmount, ReadInt(entry, "maxAmount", minAmount));
            int amount = Random.Shared.Next(minAmount, maxAmount + 1);
            if (amount > 0) {
                drops.Add(new ItemStack(itemType, checked((ushort)amount)));
            }
        }

        return drops;
    }

    public static List<ItemStack> GenerateLootFromBlock(Basalt.Core.Blocks.Block block) {
        return GenerateLootFromBlockPermutation(block.Permutation);
    }

    public static List<ItemStack> GenerateLootFromBlockPermutation(BlockPermutation permutation) {
        return GenerateLootFromBlockType(permutation.Type);
    }

    public static List<ItemStack> GenerateLootFromBlockType(BlockType blockType) {
        if (blockType.Air) {
            return [];
        }

        List<ItemStack> drops = BlockDropRegistry.GenerateDrops(blockType.Identifier);
        if (drops.Count > 0) {
            return drops;
        }

        ItemType? itemType = ItemType.Get(blockType.Identifier);
        if (itemType is null || itemType == ItemType.Air) {
            return [];
        }

        return [new ItemStack(itemType)];
    }

    private static LootTable LoadTable(string path) {
        string normalizedPath = NormalizePath(path);
        if (Tables.TryGetValue(normalizedPath, out LootTable? cached)) {
            return cached;
        }

        if (!Definitions.TryGetValue(normalizedPath, out JsonElement definition)) {
            LootTable missing = new(normalizedPath, []);
            Tables[normalizedPath] = missing;
            return missing;
        }

        List<LootPool> pools = [];
        LootTable table = new(normalizedPath, pools);
        Tables[normalizedPath] = table;

        if (definition.TryGetProperty("pools", out JsonElement poolElements) &&
            poolElements.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement poolElement in poolElements.EnumerateArray()) {
                pools.Add(ReadPool(poolElement));
            }
        }

        return table;
    }

    private static LootPool ReadPool(JsonElement element) {
        ReadRange(element, "rolls", 1, out int minRolls, out int maxRolls);
        double probability = ReadConditionsProbability(element);
        List<LootEntry> entries = [];

        if (element.TryGetProperty("entries", out JsonElement entryElements) &&
            entryElements.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement entryElement in entryElements.EnumerateArray()) {
                LootEntry? entry = ReadEntry(entryElement);
                if (entry is not null) {
                    entries.Add(entry);
                }
            }
        }

        return new LootPool(minRolls, maxRolls, probability, entries);
    }

    private static LootEntry? ReadEntry(JsonElement element) {
        string type = ReadString(element, "type");
        string name = ReadString(element, "name");
        if (string.IsNullOrWhiteSpace(type)) {
            return null;
        }

        int weight = Math.Max(1, ReadInt(element, "weight", 1));
        double probability = ReadConditionsProbability(element);
        int minCount = 1;
        int maxCount = 1;
        List<LootPool> pools = [];

        if (element.TryGetProperty("functions", out JsonElement functions) &&
            functions.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement function in functions.EnumerateArray()) {
                if (ReadString(function, "function") == "set_count") {
                    ReadRange(function, "count", 1, out minCount, out maxCount);
                }
            }
        }

        if (element.TryGetProperty("pools", out JsonElement poolElements) &&
            poolElements.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement poolElement in poolElements.EnumerateArray()) {
                pools.Add(ReadPool(poolElement));
            }
        }

        return type switch {
            "item" when !string.IsNullOrWhiteSpace(name) => new LootEntry(name, weight, probability, minCount, maxCount, null, pools),
            "loot_table" when !string.IsNullOrWhiteSpace(name) => new LootEntry(string.Empty, weight, probability, 1, 1, LoadTable(name), pools),
            "empty" => new LootEntry(string.Empty, weight, probability, 0, 0, null, pools),
            _ => null
        };
    }

    private static double ReadConditionsProbability(JsonElement element) {
        double probability = 1d;
        if (!element.TryGetProperty("conditions", out JsonElement conditions) ||
            conditions.ValueKind != JsonValueKind.Array) {
            return probability;
        }

        foreach (JsonElement condition in conditions.EnumerateArray()) {
            string name = ReadString(condition, "condition");
            if (name is "random_chance" or "random_chance_with_looting") {
                probability *= Math.Clamp(ReadDouble(condition, "chance", 1d), 0d, 1d);
            }
            else if (name is "random_difficulty_chance") {
                probability *= Math.Clamp(ReadDouble(condition, "default_chance", 1d), 0d, 1d);
            }
            else if (name is "random_regional_difficulty_chance") {
                probability *= Math.Clamp(ReadDouble(condition, "max_chance", 1d), 0d, 1d);
            }
        }

        return probability;
    }

    private static void ReadRange(JsonElement element, string property, int fallback, out int min, out int max) {
        min = fallback;
        max = fallback;

        if (!element.TryGetProperty(property, out JsonElement value)) {
            return;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int count)) {
            min = count;
            max = count;
            return;
        }

        if (value.ValueKind != JsonValueKind.Object) {
            return;
        }

        min = ReadInt(value, "min", fallback);
        max = ReadInt(value, "max", min);
        if (min > max) {
            max = min;
        }
    }

    private static string NormalizePath(string path) {
        string normalized = path.Replace('\\', '/').TrimStart('/');
        if (normalized.EndsWith(".json", StringComparison.Ordinal)) {
            normalized = normalized[..^5];
        }

        const string entityTablePrefix = "loot_tables/entities/";
        if (normalized.StartsWith(entityTablePrefix, StringComparison.Ordinal)) {
            normalized = normalized[entityTablePrefix.Length..];
        }

        return normalized;
    }

    private static string ReadString(JsonElement element, string property) {
        return element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static int ReadInt(JsonElement element, string property, int fallback) {
        return element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int result)
            ? result
            : fallback;
    }

    private static double ReadDouble(JsonElement element, string property, double fallback) {
        return element.TryGetProperty(property, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double result)
            ? result
            : fallback;
    }

    private sealed class LootTable(string path, List<LootPool> pools) {
        public string Path { get; } = path;
        private readonly List<LootPool> _pools = pools;

        public List<ItemStack> Generate() {
            List<ItemStack> items = [];
            for (int i = 0; i < _pools.Count; i++) {
                _pools[i].Generate(items);
            }

            return items;
        }
    }

    private sealed class LootPool(int minRolls, int maxRolls, double probability, List<LootEntry> entries) {
        private readonly int _minRolls = minRolls;
        private readonly int _maxRolls = maxRolls;
        private readonly double _probability = probability;
        private readonly List<LootEntry> _entries = entries;

        public void Generate(List<ItemStack> items) {
            if (_entries.Count == 0 || Random.Shared.NextDouble() > _probability) {
                return;
            }

            int rolls = Random.Shared.Next(_minRolls, _maxRolls + 1);
            for (int i = 0; i < rolls; i++) {
                LootEntry? entry = SelectEntry();
                entry?.Generate(items);
            }
        }

        private LootEntry? SelectEntry() {
            int totalWeight = 0;
            for (int i = 0; i < _entries.Count; i++) {
                totalWeight += _entries[i].Weight;
            }

            int roll = Random.Shared.Next(1, totalWeight + 1);
            for (int i = 0; i < _entries.Count; i++) {
                roll -= _entries[i].Weight;
                if (roll <= 0) {
                    return _entries[i];
                }
            }

            return null;
        }
    }

    private sealed class LootEntry(string itemIdentifier, int weight, double probability, int minCount, int maxCount, LootTable? table, List<LootPool> pools) {
        public int Weight { get; } = weight;
        private readonly string _itemIdentifier = itemIdentifier;
        private readonly double _probability = probability;
        private readonly int _minCount = minCount;
        private readonly int _maxCount = maxCount;
        private readonly LootTable? _table = table;
        private readonly List<LootPool> _pools = pools;

        public void Generate(List<ItemStack> items) {
            if (Random.Shared.NextDouble() > _probability) {
                return;
            }

            if (_table is not null) {
                items.AddRange(_table.Generate());
            }
            else if (!string.IsNullOrEmpty(_itemIdentifier)) {
                int count = Random.Shared.Next(_minCount, _maxCount + 1);
                if (count > 0 && ItemType.Get(_itemIdentifier) is ItemType itemType) {
                    items.Add(new ItemStack(itemType, checked((ushort)count)));
                }
            }

            for (int i = 0; i < _pools.Count; i++) {
                _pools[i].Generate(items);
            }
        }
    }
}
