using System;

namespace BedrockProtocol.Enums;

public enum AudioListener {
    Camera = 0,
    Player = 1,
}

public static class AudioListenerExtensions {
    public static string ToProtoString(this AudioListener value) => value.ToProtocolString();

    public static string ToProtocolString(this AudioListener value) {
        return value switch {
            AudioListener.Camera => "Camera",
            AudioListener.Player => "Player",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown AudioListener value.")
        };
    }

    public static AudioListener FromProtocolString(string value) {
        return value switch {
            "Camera" => AudioListener.Camera,
            "Player" => AudioListener.Player,
            _ => throw new ArgumentException($"Unknown AudioListener protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out AudioListener result) {
        switch (value) {
            case "Camera":
                result = AudioListener.Camera;
                return true;
            case "Player":
                result = AudioListener.Player;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
