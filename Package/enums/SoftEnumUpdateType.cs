#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum SoftEnumUpdateType {
    Add = 0,
    Remove = 1,
    Replace = 2,
}

public static class SoftEnumUpdateTypeExtensions {
    public static string ToProtoString(this SoftEnumUpdateType value) => value.ToProtocolString();

    public static string ToProtocolString(this SoftEnumUpdateType value) {
        return value switch {
            SoftEnumUpdateType.Add => "Add",
            SoftEnumUpdateType.Remove => "Remove",
            SoftEnumUpdateType.Replace => "Replace",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown SoftEnumUpdateType value.")
        };
    }

    public static SoftEnumUpdateType FromProtocolString(string value) {
        return value switch {
            "Add" => SoftEnumUpdateType.Add,
            "Remove" => SoftEnumUpdateType.Remove,
            "Replace" => SoftEnumUpdateType.Replace,
            _ => throw new ArgumentException($"Unknown SoftEnumUpdateType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out SoftEnumUpdateType result) {
        switch (value) {
            case "Add":
                result = SoftEnumUpdateType.Add;
                return true;
            case "Remove":
                result = SoftEnumUpdateType.Remove;
                return true;
            case "Replace":
                result = SoftEnumUpdateType.Replace;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
