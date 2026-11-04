using System.Text.Json;

namespace BasaltBE.Basalt.Features;

public class Feature {
    public readonly string Identifier;
    public readonly string Type;
    public readonly string[] Children;
    public readonly JsonElement Data;

    protected Feature(string identifier) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        Identifier = identifier;
        Type = string.Empty;
        Children = [];
    }

    public virtual bool Generate(FeatureContext context, int x, int y, int z) => false;

    public Feature(string identifier, string type, string[] children, JsonElement data) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(children);
        if (data.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Feature data must be a JSON object.", nameof(data));

        Identifier = identifier;
        Type = type;
        Children = children;
        Data = data.Clone();
    }
}
