#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum PlayerLocationPacketType {
    PLAYER_LOCATION_COORDINATES = 0,
    PLAYER_LOCATION_HIDE = 1,
}

public static class PlayerLocationPacketTypeExtensions {
    public static string ToProtoString(this PlayerLocationPacketType value) => value.ToProtocolString();

    public static string ToProtocolString(this PlayerLocationPacketType value) {
        return value switch {
            PlayerLocationPacketType.PLAYER_LOCATION_COORDINATES => "PLAYER_LOCATION_COORDINATES",
            PlayerLocationPacketType.PLAYER_LOCATION_HIDE => "PLAYER_LOCATION_HIDE",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PlayerLocationPacketType value.")
        };
    }

    public static PlayerLocationPacketType FromProtocolString(string value) {
        return value switch {
            "PLAYER_LOCATION_COORDINATES" => PlayerLocationPacketType.PLAYER_LOCATION_COORDINATES,
            "PLAYER_LOCATION_HIDE" => PlayerLocationPacketType.PLAYER_LOCATION_HIDE,
            _ => throw new ArgumentException($"Unknown PlayerLocationPacketType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PlayerLocationPacketType result) {
        switch (value) {
            case "PLAYER_LOCATION_COORDINATES":
                result = PlayerLocationPacketType.PLAYER_LOCATION_COORDINATES;
                return true;
            case "PLAYER_LOCATION_HIDE":
                result = PlayerLocationPacketType.PLAYER_LOCATION_HIDE;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
