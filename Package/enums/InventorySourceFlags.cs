using System;

namespace BedrockProtocol.Enums;

public enum InventorySourceFlags {
    NoFlag = 0,
    WorldInteractionRandom = 1,
}

public static class InventorySourceFlagsExtensions {
    public static string ToProtoString(this InventorySourceFlags value) => value.ToProtocolString();

    public static string ToProtocolString(this InventorySourceFlags value) {
        return value switch {
            InventorySourceFlags.NoFlag => "No Flag",
            InventorySourceFlags.WorldInteractionRandom => "World Interaction Random",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown InventorySourceFlags value.")
        };
    }

    public static InventorySourceFlags FromProtocolString(string value) {
        return value switch {
            "No Flag" => InventorySourceFlags.NoFlag,
            "World Interaction Random" => InventorySourceFlags.WorldInteractionRandom,
            _ => throw new ArgumentException($"Unknown InventorySourceFlags protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out InventorySourceFlags result) {
        switch (value) {
            case "No Flag":
                result = InventorySourceFlags.NoFlag;
                return true;
            case "World Interaction Random":
                result = InventorySourceFlags.WorldInteractionRandom;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
