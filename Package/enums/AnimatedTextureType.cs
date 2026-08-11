using System;

namespace BedrockProtocol.Enums;

public enum AnimatedTextureType {
    None = 0,
    Face = 1,
    Body32x32 = 2,
    Body128x128 = 3,
}

public static class AnimatedTextureTypeExtensions {
    public static string ToProtoString(this AnimatedTextureType value) => value.ToProtocolString();

    public static string ToProtocolString(this AnimatedTextureType value) {
        return value switch {
            AnimatedTextureType.None => "None",
            AnimatedTextureType.Face => "Face",
            AnimatedTextureType.Body32x32 => "Body32x32",
            AnimatedTextureType.Body128x128 => "Body128x128",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown AnimatedTextureType value.")
        };
    }

    public static AnimatedTextureType FromProtocolString(string value) {
        return value switch {
            "None" => AnimatedTextureType.None,
            "Face" => AnimatedTextureType.Face,
            "Body32x32" => AnimatedTextureType.Body32x32,
            "Body128x128" => AnimatedTextureType.Body128x128,
            _ => throw new ArgumentException($"Unknown AnimatedTextureType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out AnimatedTextureType result) {
        switch (value) {
            case "None":
                result = AnimatedTextureType.None;
                return true;
            case "Face":
                result = AnimatedTextureType.Face;
                return true;
            case "Body32x32":
                result = AnimatedTextureType.Body32x32;
                return true;
            case "Body128x128":
                result = AnimatedTextureType.Body128x128;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
