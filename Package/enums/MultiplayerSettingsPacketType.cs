#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum MultiplayerSettingsPacketType {
    EnableMultiplayer = 0,
    DisableMultiplayer = 1,
    RefreshJoincode = 2,
}

public static class MultiplayerSettingsPacketTypeExtensions {
    public static string ToProtoString(this MultiplayerSettingsPacketType value) => value.ToProtocolString();

    public static string ToProtocolString(this MultiplayerSettingsPacketType value) {
        return value switch {
            MultiplayerSettingsPacketType.EnableMultiplayer => "EnableMultiplayer",
            MultiplayerSettingsPacketType.DisableMultiplayer => "DisableMultiplayer",
            MultiplayerSettingsPacketType.RefreshJoincode => "RefreshJoincode",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown MultiplayerSettingsPacketType value.")
        };
    }

    public static MultiplayerSettingsPacketType FromProtocolString(string value) {
        return value switch {
            "EnableMultiplayer" => MultiplayerSettingsPacketType.EnableMultiplayer,
            "DisableMultiplayer" => MultiplayerSettingsPacketType.DisableMultiplayer,
            "RefreshJoincode" => MultiplayerSettingsPacketType.RefreshJoincode,
            _ => throw new ArgumentException($"Unknown MultiplayerSettingsPacketType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out MultiplayerSettingsPacketType result) {
        switch (value) {
            case "EnableMultiplayer":
                result = MultiplayerSettingsPacketType.EnableMultiplayer;
                return true;
            case "DisableMultiplayer":
                result = MultiplayerSettingsPacketType.DisableMultiplayer;
                return true;
            case "RefreshJoincode":
                result = MultiplayerSettingsPacketType.RefreshJoincode;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
