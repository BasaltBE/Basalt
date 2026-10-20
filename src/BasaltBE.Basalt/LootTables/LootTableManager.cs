using System.Reflection;
using System.Text.Json;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Types;

namespace BasaltBE.Basalt.LootTables;

public sealed class LootTableManager {
    static readonly string[] Variants = ["default", "silkTouch", "fortune1", "fortune2", "fortune3"];
    static readonly string[] EntityVariants = [
        "default",
        "looting1",
        "looting2",
        "looting3",
        "fireAspect1",
        "looting1_fireAspect1",
        "looting2_fireAspect1",
        "looting3_fireAspect1",
        "fireAspect2",
        "looting1_fireAspect2",
        "looting2_fireAspect2",
        "looting3_fireAspect2"
    ];
    static readonly Dictionary<string, int> ToolIndices;
    static readonly Dictionary<string, int[]> BlockDropSetIds;
    static readonly BlockDrop[][] DropSets;
    static readonly HashSet<string> RequiresToolDrops;
    static readonly Dictionary<string, int[]> EntityDropSetIds;
    static readonly EntityDrop[][] EntityDropSets;
    readonly Lock registryLock = new();
    RegistrySnapshot registry = new([], [], [], []);

    static LootTableManager() {
        Assembly dataAssembly = Assembly.Load("BasaltBE.Data");
        using Stream stream = dataAssembly.GetManifestResourceStream("BasaltBE.Data.Data.block-drops.json")
            ?? throw new InvalidOperationException("Block drop data file 'block-drops.json' is missing.");

        BlockDropTableData table = JsonSerializer.Deserialize(stream, BlockDropJsonContext.Default.BlockDropTableData)
            ?? throw new JsonException("Block drop data file 'block-drops.json' is empty.");
        if (table.Variants.Length != Variants.Length || !table.Variants.SequenceEqual(Variants, StringComparer.Ordinal))
            throw new InvalidDataException("Block drop data has an unsupported variant layout.");

        ToolIndices = new Dictionary<string, int>(table.Tools.Length * 2, StringComparer.Ordinal);
        for (int i = 0; i < table.Tools.Length; i++) {
            string tool = table.Tools[i];
            if (string.IsNullOrWhiteSpace(tool) ||
                !ToolIndices.TryAdd(tool, i) ||
                !ToolIndices.TryAdd($"minecraft:{tool}", i))
                throw new InvalidDataException($"Block drop data contains duplicate tool '{table.Tools[i]}'.");
        }

        DropSets = table.DropSets;
        for (int i = 0; i < table.DropSets.Length; i++) {
            BlockDrop[] drops = table.DropSets[i];
            for (int j = 0; j < drops.Length; j++) {
                BlockDrop drop = drops[j];
                if (string.IsNullOrWhiteSpace(drop.Identifier) || drop.MinAmount < 0 || drop.MaxAmount < drop.MinAmount)
                    throw new InvalidDataException($"Block drop set {i} contains an invalid drop entry.");
            }
        }

        BlockDropSetIds = new Dictionary<string, int[]>(table.Blocks.Length, StringComparer.Ordinal);
        int referenceCount = table.Tools.Length * Variants.Length;
        for (int i = 0; i < table.Blocks.Length; i++) {
            BlockDropBlockData block = table.Blocks[i];
            if (block.DropSetIds.Length != referenceCount)
                throw new InvalidDataException($"Block drop data for '{block.Identifier}' has an invalid lookup table.");
            if (string.IsNullOrWhiteSpace(block.Identifier) || !BlockDropSetIds.TryAdd(block.Identifier, block.DropSetIds))
                throw new InvalidDataException($"Block drop data contains duplicate block '{block.Identifier}'.");

            foreach (int dropSetId in block.DropSetIds) {
                if (dropSetId < -1 || dropSetId >= DropSets.Length)
                    throw new InvalidDataException($"Block drop data for '{block.Identifier}' contains an invalid drop set id.");
            }
        }
        RequiresToolDrops = [];
        int handIndex = ToolIndices["hand"];
        foreach (BlockDropBlockData block in table.Blocks) {
            int handDropSet = block.DropSetIds[handIndex * Variants.Length];
            if (handDropSet >= 0 && DropSets[handDropSet].Length > 0)
                continue;

            bool hasToolDrops = false;
            for (int toolIndex = 0; toolIndex < table.Tools.Length; toolIndex++) {
                if (toolIndex == handIndex)
                    continue;

                int dropSetId = block.DropSetIds[toolIndex * Variants.Length];
                if (dropSetId >= 0 && DropSets[dropSetId].Length > 0) {
                    hasToolDrops = true;
                    break;
                }
            }
            if (hasToolDrops)
                RequiresToolDrops.Add(block.Identifier);
        }

        using Stream entityStream = dataAssembly.GetManifestResourceStream("BasaltBE.Data.Data.entity-drops.json")
            ?? throw new InvalidOperationException("Entity drop data file 'entity-drops.json' is missing.");

        EntityDropTableData entityTable = JsonSerializer.Deserialize(
            entityStream,
            EntityDropJsonContext.Default.EntityDropTableData
        ) ?? throw new JsonException("Entity drop data file 'entity-drops.json' is empty.");
        if (!entityTable.Variants.SequenceEqual(EntityVariants, StringComparer.Ordinal))
            throw new InvalidDataException("Entity drop data has an unsupported variant layout.");

        EntityDropSets = entityTable.DropSets;
        for (int i = 0; i < EntityDropSets.Length; i++) {
            EntityDrop[] drops = EntityDropSets[i];
            for (int j = 0; j < drops.Length; j++) {
                EntityDrop drop = drops[j];
                if (string.IsNullOrWhiteSpace(drop.Identifier) || drop.MinAmount < 0 || drop.MaxAmount < drop.MinAmount)
                    throw new InvalidDataException($"Entity drop set {i} contains an invalid drop entry.");
            }
        }

        EntityDropSetIds = new Dictionary<string, int[]>(entityTable.Entities.Length, StringComparer.Ordinal);
        for (int i = 0; i < entityTable.Entities.Length; i++) {
            EntityDropEntityData entity = entityTable.Entities[i];
            if (entity.DropSetIds.Length != EntityVariants.Length)
                throw new InvalidDataException($"Entity drop data for '{entity.Identifier}' has an invalid lookup table.");
            if (string.IsNullOrWhiteSpace(entity.Identifier) || !EntityDropSetIds.TryAdd(entity.Identifier, entity.DropSetIds))
                throw new InvalidDataException($"Entity drop data contains duplicate entity '{entity.Identifier}'.");

            foreach (int dropSetId in entity.DropSetIds) {
                if (dropSetId < 0 || dropSetId >= EntityDropSets.Length)
                    throw new InvalidDataException($"Entity drop data for '{entity.Identifier}' contains an invalid drop set id.");
            }
        }
    }

    public ReadOnlySpan<BlockDrop> GetBlockDrops(
        string blockIdentifier,
        string toolIdentifier = "hand",
        int fortuneLevel = 0,
        bool silkTouch = false
    ) {
        ArgumentNullException.ThrowIfNull(blockIdentifier);
        ArgumentNullException.ThrowIfNull(toolIdentifier);
        if (fortuneLevel is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(fortuneLevel));

        RegistrySnapshot current = Volatile.Read(ref registry);
        if (current.CustomDrops.TryGetValue(blockIdentifier, out BlockDrop[]? customDrops))
            return customDrops;
        if (current.RemovedVanillaDrops.Contains(blockIdentifier))
            return [];

        if (!ToolIndices.TryGetValue(toolIdentifier, out int toolIndex) ||
            !BlockDropSetIds.TryGetValue(blockIdentifier, out int[]? dropSetIds))
            return [];

        int variantIndex = silkTouch ? 1 : fortuneLevel == 0 ? 0 : fortuneLevel + 1;
        int dropSetId = dropSetIds[toolIndex * Variants.Length + variantIndex];
        return dropSetId < 0 ? [] : DropSets[dropSetId];
    }

    public ReadOnlySpan<BlockDrop> Get(
        string blockIdentifier,
        ItemStack? itemStack = null,
        int fortuneLevel = 0,
        bool silkTouch = false
    ) {
        string toolIdentifier = itemStack?.Type.Identifier ?? "hand";
        if (!ToolIndices.ContainsKey(toolIdentifier) &&
            itemStack?.Type.ToolType == ToolType.Pickaxe && itemStack.Type.ToolTier == 1)
            toolIdentifier = "stone_pickaxe";
        if (!ToolIndices.ContainsKey(toolIdentifier))
            toolIdentifier = "hand";

        return GetBlockDrops(blockIdentifier, toolIdentifier, fortuneLevel, silkTouch);
    }

    public bool CanHarvest(string blockIdentifier, ItemStack? itemStack = null) {
        RegistrySnapshot current = Volatile.Read(ref registry);
        if (current.CustomDrops.ContainsKey(blockIdentifier) || current.RemovedVanillaDrops.Contains(blockIdentifier) ||
            !RequiresToolDrops.Contains(blockIdentifier))
            return true;

        return Get(blockIdentifier, itemStack).Length > 0;
    }

    public ReadOnlySpan<EntityDrop> GetEntityDrops(
        string entityIdentifier,
        int lootingLevel = 0,
        int fireAspectLevel = 0
    ) {
        ArgumentNullException.ThrowIfNull(entityIdentifier);
        if (lootingLevel is < 0 or > 3)
            throw new ArgumentOutOfRangeException(nameof(lootingLevel));
        if (fireAspectLevel is < 0 or > 2)
            throw new ArgumentOutOfRangeException(nameof(fireAspectLevel));

        RegistrySnapshot current = Volatile.Read(ref registry);
        if (current.CustomEntityDrops.TryGetValue(entityIdentifier, out EntityDrop[]? customDrops))
            return customDrops;
        if (current.RemovedVanillaEntityDrops.Contains(entityIdentifier) ||
            !EntityDropSetIds.TryGetValue(entityIdentifier, out int[]? dropSetIds))
            return [];

        int variantIndex = fireAspectLevel == 0
            ? lootingLevel
            : fireAspectLevel * 4 + lootingLevel;
        return EntityDropSets[dropSetIds[variantIndex]];
    }

    /// <summary>Registers custom drops for a block. Custom drops override every vanilla tool and enchantment variant.</summary>
    public void Register(string blockIdentifier, params BlockDrop[] drops) {
        ArgumentException.ThrowIfNullOrWhiteSpace(blockIdentifier);
        ArgumentNullException.ThrowIfNull(drops);

        BlockDrop[] registeredDrops = (BlockDrop[])drops.Clone();
        for (int i = 0; i < registeredDrops.Length; i++) {
            BlockDrop drop = registeredDrops[i];
            if (string.IsNullOrWhiteSpace(drop.Identifier) || drop.MinAmount < 0 || drop.MaxAmount < drop.MinAmount)
                throw new ArgumentException("Custom block drops must have a valid identifier and amount range.", nameof(drops));
        }

        lock (registryLock) {
            RegistrySnapshot current = Volatile.Read(ref registry);
            Dictionary<string, BlockDrop[]> customDrops = new(current.CustomDrops, StringComparer.Ordinal) {
                [blockIdentifier] = registeredDrops
            };
            HashSet<string> removedVanillaDrops = new(current.RemovedVanillaDrops, StringComparer.Ordinal);
            removedVanillaDrops.Remove(blockIdentifier);
            Volatile.Write(ref registry, new RegistrySnapshot(
                customDrops,
                removedVanillaDrops,
                current.CustomEntityDrops,
                current.RemovedVanillaEntityDrops
            ));
        }
    }

    /// <summary>Removes custom drops and disables embedded vanilla drops for this block.</summary>
    public bool Unregister(string blockIdentifier) {
        ArgumentException.ThrowIfNullOrWhiteSpace(blockIdentifier);

        lock (registryLock) {
            RegistrySnapshot current = Volatile.Read(ref registry);
            bool hadCustomDrops = current.CustomDrops.ContainsKey(blockIdentifier);
            bool hadVanillaDrops = BlockDropSetIds.ContainsKey(blockIdentifier)
                && !current.RemovedVanillaDrops.Contains(blockIdentifier);
            if (!hadCustomDrops && !hadVanillaDrops)
                return false;

            Dictionary<string, BlockDrop[]> customDrops = new(current.CustomDrops, StringComparer.Ordinal);
            customDrops.Remove(blockIdentifier);
            HashSet<string> removedVanillaDrops = new(current.RemovedVanillaDrops, StringComparer.Ordinal);
            if (BlockDropSetIds.ContainsKey(blockIdentifier))
                removedVanillaDrops.Add(blockIdentifier);
            Volatile.Write(ref registry, new RegistrySnapshot(
                customDrops,
                removedVanillaDrops,
                current.CustomEntityDrops,
                current.RemovedVanillaEntityDrops
            ));
            return true;
        }
    }

    /// <summary>Removes custom drops and restores the embedded vanilla table for this block.</summary>
    public bool RestoreVanilla(string blockIdentifier) {
        ArgumentException.ThrowIfNullOrWhiteSpace(blockIdentifier);
        if (!BlockDropSetIds.ContainsKey(blockIdentifier))
            return false;

        lock (registryLock) {
            RegistrySnapshot current = Volatile.Read(ref registry);
            bool changed = current.CustomDrops.ContainsKey(blockIdentifier)
                || current.RemovedVanillaDrops.Contains(blockIdentifier);
            if (!changed)
                return false;

            Dictionary<string, BlockDrop[]> customDrops = new(current.CustomDrops, StringComparer.Ordinal);
            customDrops.Remove(blockIdentifier);
            HashSet<string> removedVanillaDrops = new(current.RemovedVanillaDrops, StringComparer.Ordinal);
            removedVanillaDrops.Remove(blockIdentifier);
            Volatile.Write(ref registry, new RegistrySnapshot(
                customDrops,
                removedVanillaDrops,
                current.CustomEntityDrops,
                current.RemovedVanillaEntityDrops
            ));
            return true;
        }
    }

    /// <summary>Registers custom drops for an entity. Custom drops override every vanilla enchantment variant.</summary>
    public void RegisterEntity(string entityIdentifier, params EntityDrop[] drops) {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityIdentifier);
        ArgumentNullException.ThrowIfNull(drops);

        EntityDrop[] registeredDrops = (EntityDrop[])drops.Clone();
        for (int i = 0; i < registeredDrops.Length; i++) {
            EntityDrop drop = registeredDrops[i];
            if (string.IsNullOrWhiteSpace(drop.Identifier) || drop.MinAmount < 0 || drop.MaxAmount < drop.MinAmount)
                throw new ArgumentException("Custom entity drops must have a valid identifier and amount range.", nameof(drops));
        }

        lock (registryLock) {
            RegistrySnapshot current = Volatile.Read(ref registry);
            Dictionary<string, EntityDrop[]> customEntityDrops = new(current.CustomEntityDrops, StringComparer.Ordinal) {
                [entityIdentifier] = registeredDrops
            };
            HashSet<string> removedVanillaEntityDrops = new(current.RemovedVanillaEntityDrops, StringComparer.Ordinal);
            removedVanillaEntityDrops.Remove(entityIdentifier);
            Volatile.Write(ref registry, new RegistrySnapshot(
                current.CustomDrops,
                current.RemovedVanillaDrops,
                customEntityDrops,
                removedVanillaEntityDrops
            ));
        }
    }

    /// <summary>Removes custom drops and disables embedded vanilla drops for this entity.</summary>
    public bool UnregisterEntity(string entityIdentifier) {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityIdentifier);

        lock (registryLock) {
            RegistrySnapshot current = Volatile.Read(ref registry);
            bool hadCustomDrops = current.CustomEntityDrops.ContainsKey(entityIdentifier);
            bool hadVanillaDrops = EntityDropSetIds.ContainsKey(entityIdentifier)
                && !current.RemovedVanillaEntityDrops.Contains(entityIdentifier);
            if (!hadCustomDrops && !hadVanillaDrops)
                return false;

            Dictionary<string, EntityDrop[]> customEntityDrops = new(current.CustomEntityDrops, StringComparer.Ordinal);
            customEntityDrops.Remove(entityIdentifier);
            HashSet<string> removedVanillaEntityDrops = new(current.RemovedVanillaEntityDrops, StringComparer.Ordinal);
            if (EntityDropSetIds.ContainsKey(entityIdentifier))
                removedVanillaEntityDrops.Add(entityIdentifier);
            Volatile.Write(ref registry, new RegistrySnapshot(
                current.CustomDrops,
                current.RemovedVanillaDrops,
                customEntityDrops,
                removedVanillaEntityDrops
            ));
            return true;
        }
    }

    /// <summary>Removes custom drops and restores the embedded vanilla table for this entity.</summary>
    public bool RestoreVanillaEntity(string entityIdentifier) {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityIdentifier);
        if (!EntityDropSetIds.ContainsKey(entityIdentifier))
            return false;

        lock (registryLock) {
            RegistrySnapshot current = Volatile.Read(ref registry);
            bool changed = current.CustomEntityDrops.ContainsKey(entityIdentifier)
                || current.RemovedVanillaEntityDrops.Contains(entityIdentifier);
            if (!changed)
                return false;

            Dictionary<string, EntityDrop[]> customEntityDrops = new(current.CustomEntityDrops, StringComparer.Ordinal);
            customEntityDrops.Remove(entityIdentifier);
            HashSet<string> removedVanillaEntityDrops = new(current.RemovedVanillaEntityDrops, StringComparer.Ordinal);
            removedVanillaEntityDrops.Remove(entityIdentifier);
            Volatile.Write(ref registry, new RegistrySnapshot(
                current.CustomDrops,
                current.RemovedVanillaDrops,
                customEntityDrops,
                removedVanillaEntityDrops
            ));
            return true;
        }
    }

    sealed class RegistrySnapshot(
        Dictionary<string, BlockDrop[]> customDrops,
        HashSet<string> removedVanillaDrops,
        Dictionary<string, EntityDrop[]> customEntityDrops,
        HashSet<string> removedVanillaEntityDrops
    ) {
        public readonly Dictionary<string, BlockDrop[]> CustomDrops = customDrops;
        public readonly HashSet<string> RemovedVanillaDrops = removedVanillaDrops;
        public readonly Dictionary<string, EntityDrop[]> CustomEntityDrops = customEntityDrops;
        public readonly HashSet<string> RemovedVanillaEntityDrops = removedVanillaEntityDrops;
    }
}
