using System.Reflection;
using System.Text.Json;

namespace BasaltBE.Basalt.Entities.Types;

public static class EntityPalette {
    public static readonly EntityType[] Types = LoadTypes();
    static readonly Dictionary<string, EntityType> ByIdentifier = Types.ToDictionary(type => type.Identifier);
    static readonly Dictionary<int, EntityType> ByRuntimeId = Types
        .Where(type => type.RuntimeId.HasValue)
        .ToDictionary(type => type.RuntimeId!.Value);

    public static EntityType? Find(string identifier) => ByIdentifier.GetValueOrDefault(identifier);

    public static EntityType? FindByRuntimeId(int runtimeId) => ByRuntimeId.GetValueOrDefault(runtimeId);

    static EntityType[] LoadTypes() {
        Assembly dataAssembly = Assembly.Load("BasaltBE.Data");
        using Stream stream = dataAssembly.GetManifestResourceStream("BasaltBE.Data.Data.entity-types.json")
            ?? throw new InvalidOperationException("Entity data file 'entity-types.json' is missing.");

        return JsonSerializer.Deserialize<EntityType[]>(stream)
            ?? throw new JsonException("Entity data file 'entity-types.json' is empty.");
    }
}
