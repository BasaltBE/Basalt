using System.Collections.Concurrent;
using BasaltBE.Basalt.Worlds.Types;

namespace BasaltBE.Basalt.Worlds;

public sealed class ChunkGeneratorRegistry {
    readonly ConcurrentDictionary<string, Func<IChunkGenerator>> generators = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string dimensionIdentifier, Func<IChunkGenerator> createGenerator) {
        ArgumentException.ThrowIfNullOrWhiteSpace(dimensionIdentifier);
        ArgumentNullException.ThrowIfNull(createGenerator);

        if (!generators.TryAdd(dimensionIdentifier, createGenerator)) {
            throw new InvalidOperationException($"A chunk generator is already registered for dimension '{dimensionIdentifier}'.");
        }
    }

    internal IChunkGenerator CreateFor(string dimensionIdentifier) {
        if (!generators.TryGetValue(dimensionIdentifier, out Func<IChunkGenerator>? createGenerator)) {
            return new SuperflatGenerator();
        }

        return createGenerator()
            ?? throw new InvalidOperationException($"The chunk generator factory for dimension '{dimensionIdentifier}' returned null.");
    }
}
