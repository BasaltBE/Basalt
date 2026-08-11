using System;

namespace BedrockProtocol.Enums;

public enum AnimationExpression {
    Linear = 0,
    Blinking = 1,
}

public static class AnimationExpressionExtensions {
    public static string ToProtoString(this AnimationExpression value) => value.ToProtocolString();

    public static string ToProtocolString(this AnimationExpression value) {
        return value switch {
            AnimationExpression.Linear => "Linear",
            AnimationExpression.Blinking => "Blinking",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown AnimationExpression value.")
        };
    }

    public static AnimationExpression FromProtocolString(string value) {
        return value switch {
            "Linear" => AnimationExpression.Linear,
            "Blinking" => AnimationExpression.Blinking,
            _ => throw new ArgumentException($"Unknown AnimationExpression protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out AnimationExpression result) {
        switch (value) {
            case "Linear":
                result = AnimationExpression.Linear;
                return true;
            case "Blinking":
                result = AnimationExpression.Blinking;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
