#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum CommandBlockMode {
    Normal = 0,
    Repeating = 1,
    Chain = 2,
}

public static class CommandBlockModeExtensions {
    public static string ToProtoString(this CommandBlockMode value) => value.ToProtocolString();

    public static string ToProtocolString(this CommandBlockMode value) {
        return value switch {
            CommandBlockMode.Normal => "Normal",
            CommandBlockMode.Repeating => "Repeating",
            CommandBlockMode.Chain => "Chain",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown CommandBlockMode value.")
        };
    }

    public static CommandBlockMode FromProtocolString(string value) {
        return value switch {
            "Normal" => CommandBlockMode.Normal,
            "Repeating" => CommandBlockMode.Repeating,
            "Chain" => CommandBlockMode.Chain,
            _ => throw new ArgumentException($"Unknown CommandBlockMode protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out CommandBlockMode result) {
        switch (value) {
            case "Normal":
                result = CommandBlockMode.Normal;
                return true;
            case "Repeating":
                result = CommandBlockMode.Repeating;
                return true;
            case "Chain":
                result = CommandBlockMode.Chain;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
