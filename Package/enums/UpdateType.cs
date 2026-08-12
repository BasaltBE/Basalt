#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum UpdateType {
    ClearOverrides = 0,
    RemoveOverride = 1,
    SetIntOverride = 2,
    SetFloatOverride = 3,
}

public static class UpdateTypeExtensions {
    public static string ToProtoString(this UpdateType value) => value.ToProtocolString();

    public static string ToProtocolString(this UpdateType value) {
        return value switch {
            UpdateType.ClearOverrides => "ClearOverrides",
            UpdateType.RemoveOverride => "RemoveOverride",
            UpdateType.SetIntOverride => "SetIntOverride",
            UpdateType.SetFloatOverride => "SetFloatOverride",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown UpdateType value.")
        };
    }

    public static UpdateType FromProtocolString(string value) {
        return value switch {
            "ClearOverrides" => UpdateType.ClearOverrides,
            "RemoveOverride" => UpdateType.RemoveOverride,
            "SetIntOverride" => UpdateType.SetIntOverride,
            "SetFloatOverride" => UpdateType.SetFloatOverride,
            _ => throw new ArgumentException($"Unknown UpdateType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out UpdateType result) {
        switch (value) {
            case "ClearOverrides":
                result = UpdateType.ClearOverrides;
                return true;
            case "RemoveOverride":
                result = UpdateType.RemoveOverride;
                return true;
            case "SetIntOverride":
                result = UpdateType.SetIntOverride;
                return true;
            case "SetFloatOverride":
                result = UpdateType.SetFloatOverride;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
