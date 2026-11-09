using System.Collections.Concurrent;

namespace BasaltBE.Basalt.Worlds.Providers;

public sealed class WorldProviderRegistry {
    readonly ConcurrentDictionary<string, Func<string, IWorldProvider>> providers = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string identifier, Func<string, IWorldProvider> createProvider) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(createProvider);

        if (!providers.TryAdd(identifier, createProvider))
            throw new InvalidOperationException($"A world provider named '{identifier}' is already registered.");
    }

    internal IWorldProvider Create(string identifier, string worldDirectory) {
        if (!providers.TryGetValue(identifier, out Func<string, IWorldProvider>? createProvider))
            throw new KeyNotFoundException($"World provider '{identifier}' is not registered.");

        return createProvider(worldDirectory)
            ?? throw new InvalidOperationException($"The world provider factory '{identifier}' returned null.");
    }
}
