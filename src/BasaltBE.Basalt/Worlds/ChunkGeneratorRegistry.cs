using System.Collections.Concurrent;
using BasaltBE.Basalt.Worlds.Types;

namespace BasaltBE.Basalt.Worlds;

public sealed class ChunkGeneratorRegistry {
    readonly ConcurrentDictionary<string, Func<IChunkGenerator>> generators = new(StringComparer.OrdinalIgnoreCase);
    string? defaultGenerator;

    public void Register<TGenerator>(string identifier) where TGenerator : IChunkGenerator, new() =>
        Register(identifier, static () => new TGenerator());

    public void Register(string identifier, Func<IChunkGenerator> createGenerator) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(createGenerator);

        if (!generators.TryAdd(identifier, createGenerator)) {
            throw new InvalidOperationException($"A chunk generator named '{identifier}' is already registered.");
        }
    }

    public void SetDefault(string identifier) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        if (!generators.ContainsKey(identifier)) throw new KeyNotFoundException($"Chunk generator '{identifier}' is not registered.");

        defaultGenerator = identifier;
    }

    internal IChunkGenerator Create(string identifier) {
        if (!generators.TryGetValue(identifier, out Func<IChunkGenerator>? createGenerator))
            throw new KeyNotFoundException($"Chunk generator '{identifier}' is not registered.");

        return createGenerator()
            ?? throw new InvalidOperationException($"The chunk generator factory '{identifier}' returned null.");
    }

    internal IChunkGenerator CreateDefault() {
        if (defaultGenerator is null) throw new InvalidOperationException("No default chunk generator has been selected.");

        return Create(defaultGenerator);
    }
}
