#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum PartyDestinationCookieIntent {
    Notify = 0,
    OptIn = 1,
    OptOut = 2,
}

public static class PartyDestinationCookieIntentExtensions {
    public static string ToProtoString(this PartyDestinationCookieIntent value) => value.ToProtocolString();

    public static string ToProtocolString(this PartyDestinationCookieIntent value) {
        return value switch {
            PartyDestinationCookieIntent.Notify => "Notify",
            PartyDestinationCookieIntent.OptIn => "OptIn",
            PartyDestinationCookieIntent.OptOut => "OptOut",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown PartyDestinationCookieIntent value.")
        };
    }

    public static PartyDestinationCookieIntent FromProtocolString(string value) {
        return value switch {
            "Notify" => PartyDestinationCookieIntent.Notify,
            "OptIn" => PartyDestinationCookieIntent.OptIn,
            "OptOut" => PartyDestinationCookieIntent.OptOut,
            _ => throw new ArgumentException($"Unknown PartyDestinationCookieIntent protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out PartyDestinationCookieIntent result) {
        switch (value) {
            case "Notify":
                result = PartyDestinationCookieIntent.Notify;
                return true;
            case "OptIn":
                result = PartyDestinationCookieIntent.OptIn;
                return true;
            case "OptOut":
                result = PartyDestinationCookieIntent.OptOut;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
