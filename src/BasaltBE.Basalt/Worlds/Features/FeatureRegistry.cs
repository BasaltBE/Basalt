using System.Reflection;
using System.Text.Json;
using BasaltBE.Basalt.Worlds.Dimensions;

namespace BasaltBE.Basalt.Worlds.Features;

public sealed class FeatureRegistry {
    readonly Dictionary<string, Feature> features = new(StringComparer.Ordinal);
    bool frozen;

    public int Count => features.Count;
    public string[] Identifiers => features.Keys.Order(StringComparer.Ordinal).ToArray();

    public FeatureRegistry() {
        Assembly dataAssembly = Assembly.Load("BasaltBE.Data");
        using Stream source = dataAssembly.GetManifestResourceStream("BasaltBE.Data.Data.features.json")
            ?? throw new InvalidDataException("The built-in feature definitions were not found.");
        using JsonDocument document = JsonDocument.Parse(source);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("The built-in feature definitions must be an array.");

        foreach (JsonElement entry in document.RootElement.EnumerateArray()) {
            if (entry.ValueKind != JsonValueKind.Object) throw new InvalidDataException("A built-in feature must be an object.");
            JsonProperty[] types = entry.EnumerateObject()
                .Where(property => property.Name.StartsWith("minecraft:", StringComparison.Ordinal))
                .ToArray();
            if (types.Length != 1 || types[0].Value.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("A built-in feature has an invalid type definition.");

            JsonElement definition = types[0].Value;
            if (!definition.TryGetProperty("description", out JsonElement description) ||
                description.ValueKind != JsonValueKind.Object ||
                !description.TryGetProperty("identifier", out JsonElement identifier) || identifier.ValueKind != JsonValueKind.String)
                throw new InvalidDataException("A built-in feature has no valid identifier.");

            string[] children = [];
            if (definition.TryGetProperty("features", out JsonElement featureList)) {
                if (featureList.ValueKind == JsonValueKind.Array) {
                    children = featureList.EnumerateArray().Select(child => child.ValueKind switch {
                        JsonValueKind.String => child.GetString()!,
                        JsonValueKind.Array when child.GetArrayLength() > 0 => child[0].GetString()
                            ?? throw new InvalidDataException($"Feature '{identifier.GetString()}' has an invalid child identifier."),
                        _ => throw new InvalidDataException($"Feature '{identifier.GetString()}' has an invalid child identifier.")
                    }).ToArray();
                } else if (featureList.ValueKind == JsonValueKind.String) {
                    children = [featureList.GetString()
                        ?? throw new InvalidDataException($"Feature '{identifier.GetString()}' has an invalid child identifier.")];
                } else {
                    throw new InvalidDataException($"Feature '{identifier.GetString()}' has an invalid child list.");
                }
            } else if (definition.TryGetProperty("places_feature", out JsonElement placesFeature)) {
                if (placesFeature.ValueKind != JsonValueKind.String)
                    throw new InvalidDataException($"Feature '{identifier.GetString()}' has an invalid child identifier.");
                children = [placesFeature.GetString()!];
            }

            Feature feature = new(identifier.GetString()!, types[0].Name, children, entry);
            if (!features.TryAdd(feature.Identifier, feature))
                throw new InvalidDataException($"The built-in feature definitions contain duplicate identifier '{feature.Identifier}'.");
        }

    }

    public Feature? Find(string identifier) => features.GetValueOrDefault(identifier);

    public int Place(string identifier, Dimension dimension, int x, int y, int z, int? variant = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(dimension);
        if (variant is { } value) ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(variant));
        if (!features.TryGetValue(identifier, out Feature? feature))
            throw new KeyNotFoundException($"Feature '{identifier}' was not found.");
        Random random = variant is { } seed ? new Random(seed) : Random.Shared;
        return new FeaturePlacement(this, dimension, random).Place(feature, x, y, z);
    }

    public void Register(Feature feature) {
        if (frozen) throw new InvalidOperationException("Feature registrations are closed after the server starts.");
        ArgumentNullException.ThrowIfNull(feature);
        if (!features.TryAdd(feature.Identifier, feature))
            throw new InvalidOperationException($"Feature '{feature.Identifier}' is already registered.");
    }

    public bool Unregister(string identifier) {
        if (frozen) throw new InvalidOperationException("Feature registrations are closed after the server starts.");
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return features.Remove(identifier);
    }

    internal void Freeze() => frozen = true;
}
