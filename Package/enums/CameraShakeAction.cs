#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum CameraShakeAction {
    Add = 0,
    Stop = 1,
}

public static class CameraShakeActionExtensions {
    public static string ToProtoString(this CameraShakeAction value) => value.ToProtocolString();

    public static string ToProtocolString(this CameraShakeAction value) {
        return value switch {
            CameraShakeAction.Add => "Add",
            CameraShakeAction.Stop => "Stop",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown CameraShakeAction value.")
        };
    }

    public static CameraShakeAction FromProtocolString(string value) {
        return value switch {
            "Add" => CameraShakeAction.Add,
            "Stop" => CameraShakeAction.Stop,
            _ => throw new ArgumentException($"Unknown CameraShakeAction protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out CameraShakeAction result) {
        switch (value) {
            case "Add":
                result = CameraShakeAction.Add;
                return true;
            case "Stop":
                result = CameraShakeAction.Stop;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
