#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum Subtype {
    UninitializedSubtype = 0,
    EnableCommands = 1,
    DisableCommands = 2,
    UnlockWorldTemplateSettings = 3,
}

public static class SubtypeExtensions {
    public static string ToProtoString(this Subtype value) => value.ToProtocolString();

    public static string ToProtocolString(this Subtype value) {
        return value switch {
            Subtype.UninitializedSubtype => "UninitializedSubtype",
            Subtype.EnableCommands => "EnableCommands",
            Subtype.DisableCommands => "DisableCommands",
            Subtype.UnlockWorldTemplateSettings => "UnlockWorldTemplateSettings",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown Subtype value.")
        };
    }

    public static Subtype FromProtocolString(string value) {
        return value switch {
            "UninitializedSubtype" => Subtype.UninitializedSubtype,
            "EnableCommands" => Subtype.EnableCommands,
            "DisableCommands" => Subtype.DisableCommands,
            "UnlockWorldTemplateSettings" => Subtype.UnlockWorldTemplateSettings,
            _ => throw new ArgumentException($"Unknown Subtype protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out Subtype result) {
        switch (value) {
            case "UninitializedSubtype":
                result = Subtype.UninitializedSubtype;
                return true;
            case "EnableCommands":
                result = Subtype.EnableCommands;
                return true;
            case "DisableCommands":
                result = Subtype.DisableCommands;
                return true;
            case "UnlockWorldTemplateSettings":
                result = Subtype.UnlockWorldTemplateSettings;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
