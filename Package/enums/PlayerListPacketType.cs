using System;

namespace BedrockProtocol.Enums;

public enum PlayerListPacketType {
    Add = 0,
    Remove = 1,
}

public static class PlayerListPacketTypeExtensions {
    public static string ToProtoString(this PlayerListPacketType value) => value.ToProtocolString();

    public static string ToProtocolString(this PlayerListPacketType value) {
        return value switch {
            PlayerListPacketType.Add => "Add",
            PlayerListPacketType.Remove => "Remove",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PlayerListPacketType value.")
        };
    }

    public static PlayerListPacketType FromProtocolString(string value) {
        return value switch {
            "Add" => PlayerListPacketType.Add,
            "Remove" => PlayerListPacketType.Remove,
            _ => throw new ArgumentException($"Unknown PlayerListPacketType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PlayerListPacketType result) {
        switch (value) {
            case "Add":
                result = PlayerListPacketType.Add;
                return true;
            case "Remove":
                result = PlayerListPacketType.Remove;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
