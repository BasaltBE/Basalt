using System.Reflection;
using System.Text.Json;

namespace BasaltBE.Basalt.Worlds.Features;

public sealed class FeatureRuleRegistry {
    readonly Dictionary<string, FeatureRule> rules = new(StringComparer.Ordinal);
    bool frozen;

    public int Count => rules.Count;

    public FeatureRuleRegistry() {
        Assembly dataAssembly = Assembly.Load("BasaltBE.Data");
        using Stream source = dataAssembly.GetManifestResourceStream("BasaltBE.Data.Data.feature_rules.json")
            ?? throw new InvalidDataException("The built-in feature rules were not found.");
        using JsonDocument document = JsonDocument.Parse(source);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("The built-in feature rules must be an array.");

        foreach (JsonElement entry in document.RootElement.EnumerateArray()) {
            if (entry.ValueKind != JsonValueKind.Object ||
                !entry.TryGetProperty("minecraft:feature_rules", out JsonElement definition) ||
                definition.ValueKind != JsonValueKind.Object ||
                !definition.TryGetProperty("description", out JsonElement description) ||
                description.ValueKind != JsonValueKind.Object ||
                !description.TryGetProperty("identifier", out JsonElement identifier) || identifier.ValueKind != JsonValueKind.String ||
                !description.TryGetProperty("places_feature", out JsonElement feature) || feature.ValueKind != JsonValueKind.String ||
                !definition.TryGetProperty("conditions", out JsonElement conditions) ||
                conditions.ValueKind != JsonValueKind.Object ||
                !conditions.TryGetProperty("placement_pass", out JsonElement placementPass) || placementPass.ValueKind != JsonValueKind.String)
                throw new InvalidDataException("A built-in feature rule has invalid required fields.");

            if (!definition.TryGetProperty("distribution", out JsonElement distribution) ||
                distribution.ValueKind != JsonValueKind.Object ||
                !distribution.TryGetProperty("y", out JsonElement y) || y.ValueKind != JsonValueKind.String)
                throw new InvalidDataException($"Feature rule '{identifier.GetString()}' has no supported Y expression.");

            FeatureRule rule = new(
                identifier.GetString()!,
                feature.GetString()!,
                placementPass.GetString()!,
                FeatureExpression.Parse(y.GetString()!),
                entry
            );
            if (!rules.TryAdd(rule.Identifier, rule))
                throw new InvalidDataException($"The built-in feature rules contain duplicate identifier '{rule.Identifier}'.");
        }
    }

    public FeatureRule? Find(string identifier) => rules.GetValueOrDefault(identifier);

    public void Register(FeatureRule rule) {
        if (frozen) throw new InvalidOperationException("Feature rule registrations are closed after the server starts.");
        ArgumentNullException.ThrowIfNull(rule);
        if (!rules.TryAdd(rule.Identifier, rule))
            throw new InvalidOperationException($"Feature rule '{rule.Identifier}' is already registered.");
    }

    public bool Unregister(string identifier) {
        if (frozen) throw new InvalidOperationException("Feature rule registrations are closed after the server starts.");
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return rules.Remove(identifier);
    }

    internal void Freeze() => frozen = true;
}
