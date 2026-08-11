using System;

namespace BedrockProtocol.Enums;

public enum LessonAction {
    Start = 0,
    Complete = 1,
    Restart = 2,
}

public static class LessonActionExtensions {
    public static string ToProtoString(this LessonAction value) => value.ToProtocolString();

    public static string ToProtocolString(this LessonAction value) {
        return value switch {
            LessonAction.Start => "Start",
            LessonAction.Complete => "Complete",
            LessonAction.Restart => "Restart",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown LessonAction value.")
        };
    }

    public static LessonAction FromProtocolString(string value) {
        return value switch {
            "Start" => LessonAction.Start,
            "Complete" => LessonAction.Complete,
            "Restart" => LessonAction.Restart,
            _ => throw new ArgumentException($"Unknown LessonAction protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out LessonAction result) {
        switch (value) {
            case "Start":
                result = LessonAction.Start;
                return true;
            case "Complete":
                result = LessonAction.Complete;
                return true;
            case "Restart":
                result = LessonAction.Restart;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
