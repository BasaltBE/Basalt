#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum ActorLinkType {
    None = 0,
    Riding = 1,
    Passenger = 2,
}

public static class ActorLinkTypeExtensions {
    public static string ToProtoString(this ActorLinkType value) => value.ToProtocolString();

    public static string ToProtocolString(this ActorLinkType value) {
        return value switch {
            ActorLinkType.None => "None",
            ActorLinkType.Riding => "Riding",
            ActorLinkType.Passenger => "Passenger",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ActorLinkType value.")
        };
    }

    public static ActorLinkType FromProtocolString(string value) {
        return value switch {
            "None" => ActorLinkType.None,
            "Riding" => ActorLinkType.Riding,
            "Passenger" => ActorLinkType.Passenger,
            _ => throw new ArgumentException($"Unknown ActorLinkType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ActorLinkType result) {
        switch (value) {
            case "None":
                result = ActorLinkType.None;
                return true;
            case "Riding":
                result = ActorLinkType.Riding;
                return true;
            case "Passenger":
                result = ActorLinkType.Passenger;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
