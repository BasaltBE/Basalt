using System.Text.Json.Serialization;

namespace BasaltBE.Basalt.LootTables;

public readonly record struct EntityDrop(
    [property: JsonPropertyName("identifier")] string Identifier,
    [property: JsonPropertyName("minAmount")] int MinAmount,
    [property: JsonPropertyName("maxAmount")] int MaxAmount
);
