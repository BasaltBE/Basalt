using System;

namespace BedrockProtocol.Enums;

public enum InventoryLayout {
    None = 0,
    InventoryOnly = 1,
    Default = 2,
    RecipeBookOnly = 3,
}

public static class InventoryLayoutExtensions {
    public static string ToProtoString(this InventoryLayout value) => value.ToProtocolString();

    public static string ToProtocolString(this InventoryLayout value) {
        return value switch {
            InventoryLayout.None => "None",
            InventoryLayout.InventoryOnly => "InventoryOnly",
            InventoryLayout.Default => "Default",
            InventoryLayout.RecipeBookOnly => "RecipeBookOnly",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown InventoryLayout value.")
        };
    }

    public static InventoryLayout FromProtocolString(string value) {
        return value switch {
            "None" => InventoryLayout.None,
            "InventoryOnly" => InventoryLayout.InventoryOnly,
            "Default" => InventoryLayout.Default,
            "RecipeBookOnly" => InventoryLayout.RecipeBookOnly,
            _ => throw new ArgumentException($"Unknown InventoryLayout protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out InventoryLayout result) {
        switch (value) {
            case "None":
                result = InventoryLayout.None;
                return true;
            case "InventoryOnly":
                result = InventoryLayout.InventoryOnly;
                return true;
            case "Default":
                result = InventoryLayout.Default;
                return true;
            case "RecipeBookOnly":
                result = InventoryLayout.RecipeBookOnly;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
