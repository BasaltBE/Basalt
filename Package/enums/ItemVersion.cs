#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum ItemVersion {
    Legacy = 0,
    DataDriven = 1,
    None = 2,
}

public static class ItemVersionExtensions {
    public static string ToProtoString(this ItemVersion value) => value.ToProtocolString();

    public static string ToProtocolString(this ItemVersion value) {
        return value switch {
            ItemVersion.Legacy => "Legacy",
            ItemVersion.DataDriven => "DataDriven",
            ItemVersion.None => "None",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ItemVersion value.")
        };
    }

    public static ItemVersion FromProtocolString(string value) {
        return value switch {
            "Legacy" => ItemVersion.Legacy,
            "DataDriven" => ItemVersion.DataDriven,
            "None" => ItemVersion.None,
            _ => throw new ArgumentException($"Unknown ItemVersion protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ItemVersion result) {
        switch (value) {
            case "Legacy":
                result = ItemVersion.Legacy;
                return true;
            case "DataDriven":
                result = ItemVersion.DataDriven;
                return true;
            case "None":
                result = ItemVersion.None;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
