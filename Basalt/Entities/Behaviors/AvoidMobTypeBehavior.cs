namespace Basalt.Core.Entities.Behaviors;

using System.Text.Json;

public sealed class AvoidMobTypeBehavior {
    public int Priority { get; init; }
    public IReadOnlyList<AvoidMobTypeEntry> EntityTypes { get; init; } = [];

    public static AvoidMobTypeBehavior? Parse(JsonElement properties) {
        if (properties.ValueKind != JsonValueKind.Object) {
            return null;
        }

        List<AvoidMobTypeEntry> entityTypes = [];
        if (properties.TryGetProperty("entity_types", out JsonElement entries) &&
            entries.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement entry in entries.EnumerateArray()) {
                if (AvoidMobTypeEntry.Parse(entry) is { } parsed) {
                    entityTypes.Add(parsed);
                }
            }
        }

        return new AvoidMobTypeBehavior {
            Priority = ReadInt(properties, "priority") ?? int.MaxValue,
            EntityTypes = entityTypes
        };
    }

    private static int? ReadInt(JsonElement properties, string name) {
        return properties.TryGetProperty(name, out JsonElement value) &&
            value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt32(out int result)
            ? result
            : null;
    }
}

public sealed class AvoidMobTypeEntry {
    public int? MaxDistance { get; init; }
    public float WalkSpeedMultiplier { get; init; } = 1f;
    public float SprintSpeedMultiplier { get; init; } = 1f;
    public IReadOnlyList<EntityTargetFilter> Filters { get; init; } = [];

    internal static AvoidMobTypeEntry? Parse(JsonElement entry) {
        if (entry.ValueKind != JsonValueKind.Object) {
            return null;
        }

        List<EntityTargetFilter> filters = [];
        if (entry.TryGetProperty("filters", out JsonElement filterElement) &&
            EntityTargetFilter.Parse(filterElement) is { } filter) {
            filters.Add(filter);
        }

        return new AvoidMobTypeEntry {
            MaxDistance = ReadInt(entry, "max_dist"),
            WalkSpeedMultiplier = ReadFloat(entry, "walk_speed_multiplier") ?? 1f,
            SprintSpeedMultiplier = ReadFloat(entry, "sprint_speed_multiplier") ?? 1f,
            Filters = filters
        };
    }

    private static int? ReadInt(JsonElement properties, string name) {
        return properties.TryGetProperty(name, out JsonElement value) &&
            value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt32(out int result)
            ? result
            : null;
    }

    private static float? ReadFloat(JsonElement properties, string name) {
        if (!properties.TryGetProperty(name, out JsonElement value)) {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out float number)) {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String && float.TryParse(value.GetString(), out float text)) {
            return text;
        }

        return null;
    }
}
