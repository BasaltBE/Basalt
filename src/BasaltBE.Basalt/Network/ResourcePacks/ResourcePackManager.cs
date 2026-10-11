using System.Text.Json;
using BasaltBE.Basalt.Profiling;

namespace BasaltBE.Basalt.Network.ResourcePacks;

internal sealed class ResourcePackManager {
    public const uint ChunkSize = 131_072;
    public static readonly ResourcePackManager Empty = new([], false);
    private static readonly JsonSerializerOptions ConfigurationJsonOptions = new() {
        IncludeFields = true,
        WriteIndented = true
    };
    private readonly Dictionary<Guid, ResourcePack> packsById;

    private ResourcePackManager(ResourcePack[] packs, bool required) {
        Packs = packs;
        Required = required;
        packsById = new Dictionary<Guid, ResourcePack>(packs.Length);
        foreach (ResourcePack pack in packs)
            packsById.Add(pack.Id, pack);
    }

    public readonly ResourcePack[] Packs;
    public readonly bool Required;

    public static ResourcePackManager Load(string directory = "resource_packs") {
        using ProfilerZone zone = Profiler.BeginZone("ResourcePacks.Load");
        Directory.CreateDirectory(directory);
        List<ResourcePack> packs = [];
        HashSet<Guid> ids = [];
        foreach (string packDirectory in Directory.EnumerateDirectories(directory).Order(StringComparer.Ordinal)) {
            if (!File.Exists(Path.Combine(packDirectory, "manifest.json"))) continue;
            ResourcePack pack = ResourcePack.Load(packDirectory);
            if (!ids.Add(pack.Id))
                throw new InvalidDataException($"Resource pack UUID '{pack.Id}' is duplicated.");
            packs.Add(pack);
        }

        string configurationPath = Path.Combine(directory, "config.json");
        ResourcePackConfiguration configuration;
        if (File.Exists(configurationPath)) {
            using FileStream configurationStream = File.OpenRead(configurationPath);
            configuration = JsonSerializer.Deserialize<ResourcePackConfiguration>(configurationStream, ConfigurationJsonOptions)
                ?? throw new InvalidDataException($"Resource pack configuration is empty: {configurationPath}.");
        }
        else {
            configuration = new ResourcePackConfiguration {
                EnabledPacks = new string[packs.Count]
            };
            for (int i = 0; i < packs.Count; i++)
                configuration.EnabledPacks[i] = packs[i].Id.ToString();
            File.WriteAllText(configurationPath, JsonSerializer.Serialize(configuration, ConfigurationJsonOptions));
        }

        if (configuration.EnabledPacks is null)
            return new ResourcePackManager([.. packs], configuration.Required);

        HashSet<Guid> enabledIds = [];
        foreach (string packId in configuration.EnabledPacks) {
            if (!Guid.TryParse(packId, out Guid id))
                throw new InvalidDataException($"Resource pack configuration has an invalid pack UUID '{packId}'.");
            enabledIds.Add(id);
        }

        ResourcePack[] enabledPacks = new ResourcePack[enabledIds.Count];
        int enabledCount = 0;
        foreach (ResourcePack pack in packs)
            if (enabledIds.Contains(pack.Id)) enabledPacks[enabledCount++] = pack;
        Array.Resize(ref enabledPacks, enabledCount);
        return new ResourcePackManager(enabledPacks, configuration.Required);
    }

    public bool TryGet(Guid id, out ResourcePack? pack) => packsById.TryGetValue(id, out pack);

    public bool TryGet(string resourceName, out ResourcePack? pack) {
        int versionSeparator = resourceName.IndexOf('_');
        ReadOnlySpan<char> id = versionSeparator < 0 ? resourceName.AsSpan() : resourceName.AsSpan(0, versionSeparator);
        if (Guid.TryParse(id, out Guid packId) && packsById.TryGetValue(packId, out pack)) return true;
        pack = null;
        return false;
    }
}
