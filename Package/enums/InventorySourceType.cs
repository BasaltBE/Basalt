#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum InventorySourceType {
    ContainerInventory = 0,
    GlobalInventory = 1,
    WorldInteraction = 2,
    CreativeInventory = 3,
    NonImplementedFeatureTODO = 99999,
}

public static class InventorySourceTypeExtensions {
    public static string ToProtoString(this InventorySourceType value) => value.ToProtocolString();

    public static string ToProtocolString(this InventorySourceType value) {
        return value switch {
            InventorySourceType.ContainerInventory => "Container Inventory",
            InventorySourceType.GlobalInventory => "Global Inventory",
            InventorySourceType.WorldInteraction => "World Interaction",
            InventorySourceType.CreativeInventory => "Creative Inventory",
            InventorySourceType.NonImplementedFeatureTODO => "Non Implemented Feature TODO",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown InventorySourceType value.")
        };
    }

    public static InventorySourceType FromProtocolString(string value) {
        return value switch {
            "Container Inventory" => InventorySourceType.ContainerInventory,
            "Global Inventory" => InventorySourceType.GlobalInventory,
            "World Interaction" => InventorySourceType.WorldInteraction,
            "Creative Inventory" => InventorySourceType.CreativeInventory,
            "Non Implemented Feature TODO" => InventorySourceType.NonImplementedFeatureTODO,
            _ => throw new ArgumentException($"Unknown InventorySourceType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out InventorySourceType result) {
        switch (value) {
            case "Container Inventory":
                result = InventorySourceType.ContainerInventory;
                return true;
            case "Global Inventory":
                result = InventorySourceType.GlobalInventory;
                return true;
            case "World Interaction":
                result = InventorySourceType.WorldInteraction;
                return true;
            case "Creative Inventory":
                result = InventorySourceType.CreativeInventory;
                return true;
            case "Non Implemented Feature TODO":
                result = InventorySourceType.NonImplementedFeatureTODO;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
