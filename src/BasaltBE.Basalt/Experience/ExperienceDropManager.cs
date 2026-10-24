using System.Reflection;
using System.Text.Json;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Experience;

public sealed class ExperienceDropManager {
    private static readonly int[] OrbValues = [2477, 1237, 617, 307, 149, 73, 37, 17, 7, 3, 1];
    private readonly Lock registryLock = new();
    private RegistrySnapshot registry;

    public ExperienceDropManager() {
        Assembly dataAssembly = Assembly.Load("BasaltBE.Data");
        using Stream stream = dataAssembly.GetManifestResourceStream("BasaltBE.Data.Data.xp-drops.json")
            ?? throw new InvalidOperationException("Experience drop data file 'xp-drops.json' is missing.");
        ExperienceDropTableData table = JsonSerializer.Deserialize(
            stream,
            ExperienceDropJsonContext.Default.ExperienceDropTableData
        ) ?? throw new JsonException("Experience drop data file 'xp-drops.json' is empty.");

        Dictionary<string, ExperienceRange> blocks = LoadRanges(table.Blocks, "block");
        Dictionary<string, ExperienceRange> entities = LoadRanges(table.Entities, "entity");
        Dictionary<string, float> smelt = new(table.Smelt.Length, StringComparer.Ordinal);
        foreach (SmeltExperience entry in table.Smelt) {
            if (string.IsNullOrWhiteSpace(entry.Identifier) || !float.IsFinite(entry.Experience) || entry.Experience < 0 ||
                !smelt.TryAdd(entry.Identifier, entry.Experience))
                throw new InvalidDataException($"Experience drop data contains an invalid smelt entry '{entry.Identifier}'.");
        }

        registry = new RegistrySnapshot(blocks, entities, smelt, [], [], [], [], [], []);
    }

    public int GetBlockExperience(string identifier) {
        RegistrySnapshot current = Volatile.Read(ref registry);
        return Roll(identifier, current.Blocks, current.CustomBlocks, current.RemovedBlocks);
    }

    public int GetEntityExperience(string identifier) {
        RegistrySnapshot current = Volatile.Read(ref registry);
        return Roll(identifier, current.Entities, current.CustomEntities, current.RemovedEntities);
    }

    public float GetSmeltExperience(string identifier) {
        RegistrySnapshot current = Volatile.Read(ref registry);
        if (current.CustomSmelt.TryGetValue(identifier, out float custom))
            return custom;
        if (current.RemovedSmelt.Contains(identifier))
            return 0;
        return current.Smelt.GetValueOrDefault(identifier);
    }

    public void Register(ExpDropType type, string identifier, int minimum, int maximum) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        if (minimum < 0 || maximum < minimum || maximum == int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(minimum));
        if (type is not (ExpDropType.Block or ExpDropType.Entity))
            throw new ArgumentOutOfRangeException(nameof(type));

        lock (registryLock) {
            RegistrySnapshot current = registry;
            bool block = type == ExpDropType.Block;
            Dictionary<string, ExperienceRange> custom = new(
                block ? current.CustomBlocks : current.CustomEntities,
                StringComparer.Ordinal
            );
            HashSet<string> removed = new(
                block ? current.RemovedBlocks : current.RemovedEntities,
                StringComparer.Ordinal
            );
            custom[identifier] = new ExperienceRange { Identifier = identifier, Minimum = minimum, Maximum = maximum };
            removed.Remove(identifier);
            Volatile.Write(ref registry, block
                ? current with { CustomBlocks = custom, RemovedBlocks = removed }
                : current with { CustomEntities = custom, RemovedEntities = removed });
        }
    }

    public void Register(ExpDropType type, string identifier, float experience) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        if (type != ExpDropType.Smelting)
            throw new ArgumentOutOfRangeException(nameof(type));
        if (!float.IsFinite(experience) || experience < 0)
            throw new ArgumentOutOfRangeException(nameof(experience));

        lock (registryLock) {
            RegistrySnapshot current = registry;
            Dictionary<string, float> custom = new(current.CustomSmelt, StringComparer.Ordinal);
            custom[identifier] = experience;
            HashSet<string> removed = new(current.RemovedSmelt, StringComparer.Ordinal);
            removed.Remove(identifier);
            Volatile.Write(ref registry, current with { CustomSmelt = custom, RemovedSmelt = removed });
        }
    }

    public bool Unregister(ExpDropType type, string identifier) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        lock (registryLock) {
            RegistrySnapshot current = registry;
            switch (type) {
                case ExpDropType.Block: {
                        bool exists = current.CustomBlocks.ContainsKey(identifier) || current.Blocks.ContainsKey(identifier);
                        if (!exists)
                            return false;
                        Dictionary<string, ExperienceRange> custom = new(current.CustomBlocks, StringComparer.Ordinal);
                        custom.Remove(identifier);
                        HashSet<string> removed = new(current.RemovedBlocks, StringComparer.Ordinal);
                        if (current.Blocks.ContainsKey(identifier))
                            removed.Add(identifier);
                        Volatile.Write(ref registry, current with { CustomBlocks = custom, RemovedBlocks = removed });
                        return true;
                    }
                case ExpDropType.Entity: {
                        bool exists = current.CustomEntities.ContainsKey(identifier) || current.Entities.ContainsKey(identifier);
                        if (!exists)
                            return false;
                        Dictionary<string, ExperienceRange> custom = new(current.CustomEntities, StringComparer.Ordinal);
                        custom.Remove(identifier);
                        HashSet<string> removed = new(current.RemovedEntities, StringComparer.Ordinal);
                        if (current.Entities.ContainsKey(identifier))
                            removed.Add(identifier);
                        Volatile.Write(ref registry, current with { CustomEntities = custom, RemovedEntities = removed });
                        return true;
                    }
                case ExpDropType.Smelting: {
                        bool exists = current.CustomSmelt.ContainsKey(identifier) || current.Smelt.ContainsKey(identifier);
                        if (!exists)
                            return false;
                        Dictionary<string, float> custom = new(current.CustomSmelt, StringComparer.Ordinal);
                        custom.Remove(identifier);
                        HashSet<string> removed = new(current.RemovedSmelt, StringComparer.Ordinal);
                        if (current.Smelt.ContainsKey(identifier))
                            removed.Add(identifier);
                        Volatile.Write(ref registry, current with { CustomSmelt = custom, RemovedSmelt = removed });
                        return true;
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }

    public void Drop(Dimension dimension, Vec3 position, int amount) {
        ArgumentNullException.ThrowIfNull(dimension);
        ArgumentNullException.ThrowIfNull(position);
        if (amount <= 0)
            return;

        foreach (int value in OrbValues) {
            while (amount >= value) {
                Entity orb = new("minecraft:xp_orb");
                orb.Position.X = position.X;
                orb.Position.Y = position.Y;
                orb.Position.Z = position.Z;
                orb.GetTrait<ExperienceOrbTrait>()!.ExperienceValue = value;
                orb.Spawn(dimension);
                amount -= value;
            }
        }
    }

    private static Dictionary<string, ExperienceRange> LoadRanges(ExperienceRange[] entries, string kind) {
        Dictionary<string, ExperienceRange> result = new(entries.Length, StringComparer.Ordinal);
        foreach (ExperienceRange entry in entries) {
            if (string.IsNullOrWhiteSpace(entry.Identifier) || entry.Minimum < 0 || entry.Maximum < entry.Minimum ||
                entry.Maximum == int.MaxValue || !result.TryAdd(entry.Identifier, entry))
                throw new InvalidDataException($"Experience drop data contains an invalid {kind} entry '{entry.Identifier}'.");
        }
        return result;
    }

    private static int Roll(
        string identifier,
        Dictionary<string, ExperienceRange> vanilla,
        Dictionary<string, ExperienceRange> custom,
        HashSet<string> removed
    ) {
        if (custom.TryGetValue(identifier, out ExperienceRange? entry))
            return Random.Shared.Next(entry.Minimum, entry.Maximum + 1);
        if (removed.Contains(identifier) || !vanilla.TryGetValue(identifier, out entry))
            return 0;
        return Random.Shared.Next(entry.Minimum, entry.Maximum + 1);
    }

    private sealed record RegistrySnapshot(
        Dictionary<string, ExperienceRange> Blocks,
        Dictionary<string, ExperienceRange> Entities,
        Dictionary<string, float> Smelt,
        Dictionary<string, ExperienceRange> CustomBlocks,
        Dictionary<string, ExperienceRange> CustomEntities,
        Dictionary<string, float> CustomSmelt,
        HashSet<string> RemovedBlocks,
        HashSet<string> RemovedEntities,
        HashSet<string> RemovedSmelt
    );
}
