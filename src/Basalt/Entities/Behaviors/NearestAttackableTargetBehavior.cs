namespace Basalt.Core.Entities.Behaviors;

using System.Text.Json;

public sealed class NearestAttackableTargetBehavior {
    public int Priority { get; init; }
    public bool ReselectTargets { get; init; }
    public bool SetPersistent { get; init; }
    public bool MustSee { get; init; }
    public bool MustReach { get; init; }
    public int? WithinRadius { get; init; }
    public int? MustSeeForgetDuration { get; init; }
    public int? AttackIntervalMax { get; init; }
    public IReadOnlyList<NearestAttackableTargetEntry> EntityTypes { get; init; } = [];

    public static NearestAttackableTargetBehavior? Parse(JsonElement properties) {
        if (properties.ValueKind != JsonValueKind.Object) {
            return null;
        }

        List<NearestAttackableTargetEntry> entityTypes = [];
        if (properties.TryGetProperty("entity_types", out JsonElement entries) &&
            entries.ValueKind == JsonValueKind.Array) {
            foreach (JsonElement entry in entries.EnumerateArray()) {
                if (NearestAttackableTargetEntry.Parse(entry) is { } parsed) {
                    entityTypes.Add(parsed);
                }
            }
        }

        return new NearestAttackableTargetBehavior {
            Priority = ReadInt(properties, "priority") ?? int.MaxValue,
            ReselectTargets = ReadBool(properties, "reselect_targets"),
            SetPersistent = ReadBool(properties, "set_persistent"),
            MustSee = ReadBool(properties, "must_see"),
            MustReach = ReadBool(properties, "must_reach"),
            WithinRadius = ReadInt(properties, "within_radius"),
            MustSeeForgetDuration = ReadInt(properties, "must_see_forget_duration"),
            AttackIntervalMax = ReadIntervalMax(properties),
            EntityTypes = entityTypes
        };
    }

    private static bool ReadBool(JsonElement properties, string name) {
        return properties.TryGetProperty(name, out JsonElement value) &&
            value.ValueKind == JsonValueKind.True;
    }

    private static int? ReadInt(JsonElement properties, string name) {
        return properties.TryGetProperty(name, out JsonElement value) &&
            value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt32(out int result)
            ? result
            : null;
    }

    private static int? ReadIntervalMax(JsonElement properties) {
        if (!properties.TryGetProperty("attack_interval", out JsonElement interval) ||
            interval.ValueKind != JsonValueKind.Object) {
            return null;
        }

        return ReadInt(interval, "max");
    }
}

public sealed class NearestAttackableTargetEntry {
    public int? MaxDistance { get; init; }
    public bool? MustSee { get; init; }
    public IReadOnlyList<EntityTargetFilter> Filters { get; init; } = [];

    internal static NearestAttackableTargetEntry? Parse(JsonElement entry) {
        if (entry.ValueKind != JsonValueKind.Object) {
            return null;
        }

        List<EntityTargetFilter> filters = [];
        if (entry.TryGetProperty("filters", out JsonElement filterElement) &&
            EntityTargetFilter.Parse(filterElement) is { } filter) {
            filters.Add(filter);
        }

        return new NearestAttackableTargetEntry {
            MaxDistance = ReadInt(entry, "max_dist"),
            MustSee = ReadNullableBool(entry, "must_see"),
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

    private static bool? ReadNullableBool(JsonElement properties, string name) {
        if (!properties.TryGetProperty(name, out JsonElement value)) {
            return null;
        }

        return value.ValueKind switch {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }
}

public sealed class EntityTargetFilter {
    public string? Test { get; init; }
    public int? Subject { get; init; }
    public int? Operator { get; init; }
    public JsonElement Value { get; init; }
    public IReadOnlyList<EntityTargetFilter> All { get; init; } = [];
    public IReadOnlyList<EntityTargetFilter> Any { get; init; } = [];

    internal static EntityTargetFilter? Parse(JsonElement element) {
        if (element.ValueKind != JsonValueKind.Object) {
            return null;
        }

        List<EntityTargetFilter> all = ParseGroup(element, "AND");
        List<EntityTargetFilter> any = ParseGroup(element, "OR");
        string? test = element.TryGetProperty("test", out JsonElement testValue) &&
            testValue.ValueKind == JsonValueKind.String
            ? testValue.GetString()
            : null;

        return new EntityTargetFilter {
            Test = test,
            Subject = ReadInt(element, "subject"),
            Operator = ReadInt(element, "operator"),
            Value = element.TryGetProperty("value", out JsonElement value) ? value : default,
            All = all,
            Any = any
        };
    }

    private static List<EntityTargetFilter> ParseGroup(JsonElement element, string name) {
        List<EntityTargetFilter> filters = [];
        if (!element.TryGetProperty(name, out JsonElement values) || values.ValueKind != JsonValueKind.Array) {
            return filters;
        }

        foreach (JsonElement value in values.EnumerateArray()) {
            if (Parse(value) is { } filter) {
                filters.Add(filter);
            }
        }

        return filters;
    }

    private static int? ReadInt(JsonElement properties, string name) {
        return properties.TryGetProperty(name, out JsonElement value) &&
            value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt32(out int result)
            ? result
            : null;
    }
}
