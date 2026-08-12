#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum personaArmSizeType {
    Slim = 0,
    Wide = 1,
}

public static class personaArmSizeTypeExtensions {
    public static string ToProtoString(this personaArmSizeType value) => value.ToProtocolString();

    public static string ToProtocolString(this personaArmSizeType value) {
        return value switch {
            personaArmSizeType.Slim => "Slim",
            personaArmSizeType.Wide => "Wide",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown personaArmSizeType value.")
        };
    }

    public static personaArmSizeType FromProtocolString(string value) {
        return value switch {
            "Slim" => personaArmSizeType.Slim,
            "Wide" => personaArmSizeType.Wide,
            _ => throw new ArgumentException($"Unknown personaArmSizeType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out personaArmSizeType result) {
        switch (value) {
            case "Slim":
                result = personaArmSizeType.Slim;
                return true;
            case "Wide":
                result = personaArmSizeType.Wide;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
