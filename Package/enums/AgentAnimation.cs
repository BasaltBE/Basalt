#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum AgentAnimation {
    ArmSwing = 0,
    Shrug = 1,
}

public static class AgentAnimationExtensions {
    public static string ToProtoString(this AgentAnimation value) => value.ToProtocolString();

    public static string ToProtocolString(this AgentAnimation value) {
        return value switch {
            AgentAnimation.ArmSwing => "ArmSwing",
            AgentAnimation.Shrug => "Shrug",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown AgentAnimation value.")
        };
    }

    public static AgentAnimation FromProtocolString(string value) {
        return value switch {
            "ArmSwing" => AgentAnimation.ArmSwing,
            "Shrug" => AgentAnimation.Shrug,
            _ => throw new ArgumentException($"Unknown AgentAnimation protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out AgentAnimation result) {
        switch (value) {
            case "ArmSwing":
                result = AgentAnimation.ArmSwing;
                return true;
            case "Shrug":
                result = AgentAnimation.Shrug;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
