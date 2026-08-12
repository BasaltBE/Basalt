#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum AnimationMode {
    None = 0,
    Layers = 1,
    Blocks = 2,
}

public static class AnimationModeExtensions {
    public static string ToProtoString(this AnimationMode value) => value.ToProtocolString();

    public static string ToProtocolString(this AnimationMode value) {
        return value switch {
            AnimationMode.None => "None",
            AnimationMode.Layers => "Layers",
            AnimationMode.Blocks => "Blocks",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown AnimationMode value.")
        };
    }

    public static AnimationMode FromProtocolString(string value) {
        return value switch {
            "None" => AnimationMode.None,
            "Layers" => AnimationMode.Layers,
            "Blocks" => AnimationMode.Blocks,
            _ => throw new ArgumentException($"Unknown AnimationMode protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out AnimationMode result) {
        switch (value) {
            case "None":
                result = AnimationMode.None;
                return true;
            case "Layers":
                result = AnimationMode.Layers;
                return true;
            case "Blocks":
                result = AnimationMode.Blocks;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
