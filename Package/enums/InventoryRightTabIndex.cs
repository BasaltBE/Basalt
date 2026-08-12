#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum InventoryRightTabIndex {
    None = 0,
    FullScreen = 1,
    Crafting = 2,
    Armor = 3,
}

public static class InventoryRightTabIndexExtensions {
    public static string ToProtoString(this InventoryRightTabIndex value) => value.ToProtocolString();

    public static string ToProtocolString(this InventoryRightTabIndex value) {
        return value switch {
            InventoryRightTabIndex.None => "None",
            InventoryRightTabIndex.FullScreen => "FullScreen",
            InventoryRightTabIndex.Crafting => "Crafting",
            InventoryRightTabIndex.Armor => "Armor",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown InventoryRightTabIndex value.")
        };
    }

    public static InventoryRightTabIndex FromProtocolString(string value) {
        return value switch {
            "None" => InventoryRightTabIndex.None,
            "FullScreen" => InventoryRightTabIndex.FullScreen,
            "Crafting" => InventoryRightTabIndex.Crafting,
            "Armor" => InventoryRightTabIndex.Armor,
            _ => throw new ArgumentException($"Unknown InventoryRightTabIndex protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out InventoryRightTabIndex result) {
        switch (value) {
            case "None":
                result = InventoryRightTabIndex.None;
                return true;
            case "FullScreen":
                result = InventoryRightTabIndex.FullScreen;
                return true;
            case "Crafting":
                result = InventoryRightTabIndex.Crafting;
                return true;
            case "Armor":
                result = InventoryRightTabIndex.Armor;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
