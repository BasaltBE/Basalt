#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum CreditsState {
    Start = 0,
    Finished = 1,
}

public static class CreditsStateExtensions {
    public static string ToProtoString(this CreditsState value) => value.ToProtocolString();

    public static string ToProtocolString(this CreditsState value) {
        return value switch {
            CreditsState.Start => "Start",
            CreditsState.Finished => "Finished",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown CreditsState value.")
        };
    }

    public static CreditsState FromProtocolString(string value) {
        return value switch {
            "Start" => CreditsState.Start,
            "Finished" => CreditsState.Finished,
            _ => throw new ArgumentException($"Unknown CreditsState protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out CreditsState result) {
        switch (value) {
            case "Start":
                result = CreditsState.Start;
                return true;
            case "Finished":
                result = CreditsState.Finished;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
