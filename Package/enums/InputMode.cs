#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum InputMode {
    Undefined = 0,
    Mouse = 1,
    Touch = 2,
    GamePad = 3,
    Count = 5,
}

public static class InputModeExtensions {
    public static string ToProtoString(this InputMode value) => value.ToProtocolString();

    public static string ToProtocolString(this InputMode value) {
        return value switch {
            InputMode.Undefined => "Undefined",
            InputMode.Mouse => "Mouse",
            InputMode.Touch => "Touch",
            InputMode.GamePad => "GamePad",
            InputMode.Count => "Count",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown InputMode value.")
        };
    }

    public static InputMode FromProtocolString(string value) {
        return value switch {
            "Undefined" => InputMode.Undefined,
            "Mouse" => InputMode.Mouse,
            "Touch" => InputMode.Touch,
            "GamePad" => InputMode.GamePad,
            "Count" => InputMode.Count,
            _ => throw new ArgumentException($"Unknown InputMode protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out InputMode result) {
        switch (value) {
            case "Undefined":
                result = InputMode.Undefined;
                return true;
            case "Mouse":
                result = InputMode.Mouse;
                return true;
            case "Touch":
                result = InputMode.Touch;
                return true;
            case "GamePad":
                result = InputMode.GamePad;
                return true;
            case "Count":
                result = InputMode.Count;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
