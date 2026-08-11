using System;

namespace BedrockProtocol.Enums;

public enum CommandOutputType {
    None = 0,
    LastOutput = 1,
    Silent = 2,
    AllOutput = 3,
    DataSet = 4,
}

public static class CommandOutputTypeExtensions {
    public static string ToProtoString(this CommandOutputType value) => value.ToProtocolString();

    public static string ToProtocolString(this CommandOutputType value) {
        return value switch {
            CommandOutputType.None => "None",
            CommandOutputType.LastOutput => "LastOutput",
            CommandOutputType.Silent => "Silent",
            CommandOutputType.AllOutput => "AllOutput",
            CommandOutputType.DataSet => "DataSet",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown CommandOutputType value.")
        };
    }

    public static CommandOutputType FromProtocolString(string value) {
        return value switch {
            "None" => CommandOutputType.None,
            "LastOutput" => CommandOutputType.LastOutput,
            "Silent" => CommandOutputType.Silent,
            "AllOutput" => CommandOutputType.AllOutput,
            "DataSet" => CommandOutputType.DataSet,
            _ => throw new ArgumentException($"Unknown CommandOutputType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out CommandOutputType result) {
        switch (value) {
            case "None":
                result = CommandOutputType.None;
                return true;
            case "LastOutput":
                result = CommandOutputType.LastOutput;
                return true;
            case "Silent":
                result = CommandOutputType.Silent;
                return true;
            case "AllOutput":
                result = CommandOutputType.AllOutput;
                return true;
            case "DataSet":
                result = CommandOutputType.DataSet;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
