using System;

namespace BedrockProtocol.Enums;

public enum ServerboundLoadingScreenPacketType {
    Unknown = 0,
    StartLoadingScreen = 1,
    EndLoadingScreen = 2,
}

public static class ServerboundLoadingScreenPacketTypeExtensions {
    public static string ToProtoString(this ServerboundLoadingScreenPacketType value) => value.ToProtocolString();

    public static string ToProtocolString(this ServerboundLoadingScreenPacketType value) {
        return value switch {
            ServerboundLoadingScreenPacketType.Unknown => "Unknown",
            ServerboundLoadingScreenPacketType.StartLoadingScreen => "StartLoadingScreen",
            ServerboundLoadingScreenPacketType.EndLoadingScreen => "EndLoadingScreen",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ServerboundLoadingScreenPacketType value.")
        };
    }

    public static ServerboundLoadingScreenPacketType FromProtocolString(string value) {
        return value switch {
            "Unknown" => ServerboundLoadingScreenPacketType.Unknown,
            "StartLoadingScreen" => ServerboundLoadingScreenPacketType.StartLoadingScreen,
            "EndLoadingScreen" => ServerboundLoadingScreenPacketType.EndLoadingScreen,
            _ => throw new ArgumentException($"Unknown ServerboundLoadingScreenPacketType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ServerboundLoadingScreenPacketType result) {
        switch (value) {
            case "Unknown":
                result = ServerboundLoadingScreenPacketType.Unknown;
                return true;
            case "StartLoadingScreen":
                result = ServerboundLoadingScreenPacketType.StartLoadingScreen;
                return true;
            case "EndLoadingScreen":
                result = ServerboundLoadingScreenPacketType.EndLoadingScreen;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
