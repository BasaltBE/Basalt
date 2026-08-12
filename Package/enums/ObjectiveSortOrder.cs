#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum ObjectiveSortOrder {
    Ascending = 0,
    Descending = 1,
}

public static class ObjectiveSortOrderExtensions {
    public static string ToProtoString(this ObjectiveSortOrder value) => value.ToProtocolString();

    public static string ToProtocolString(this ObjectiveSortOrder value) {
        return value switch {
            ObjectiveSortOrder.Ascending => "Ascending",
            ObjectiveSortOrder.Descending => "Descending",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ObjectiveSortOrder value.")
        };
    }

    public static ObjectiveSortOrder FromProtocolString(string value) {
        return value switch {
            "Ascending" => ObjectiveSortOrder.Ascending,
            "Descending" => ObjectiveSortOrder.Descending,
            _ => throw new ArgumentException($"Unknown ObjectiveSortOrder protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ObjectiveSortOrder result) {
        switch (value) {
            case "Ascending":
                result = ObjectiveSortOrder.Ascending;
                return true;
            case "Descending":
                result = ObjectiveSortOrder.Descending;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
