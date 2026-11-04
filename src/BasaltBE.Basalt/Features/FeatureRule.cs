using System.Text.Json;

namespace BasaltBE.Basalt.Features;

public sealed class FeatureRule {
    public readonly string Identifier;
    public readonly string FeatureIdentifier;
    public readonly string PlacementPass;
    public readonly FeatureExpression YExpression;
    public readonly JsonElement Data;

    public FeatureRule(string identifier, string featureIdentifier, string placementPass, FeatureExpression yExpression, JsonElement data) {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(featureIdentifier);
        ArgumentException.ThrowIfNullOrWhiteSpace(placementPass);
        if (!yExpression.Valid)
            throw new ArgumentException("A valid Y expression is required.", nameof(yExpression));
        if (data.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Feature rule data must be a JSON object.", nameof(data));

        Identifier = identifier;
        FeatureIdentifier = featureIdentifier;
        PlacementPass = placementPass;
        YExpression = yExpression;
        Data = data.Clone();
    }
}
