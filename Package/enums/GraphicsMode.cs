using System;

namespace BedrockProtocol.Enums;

public enum GraphicsMode {
    Simple = 0,
    Fancy = 1,
    Advanced = 2,
    RayTraced = 3,
}

public static class GraphicsModeExtensions {
    public static string ToProtoString(this GraphicsMode value) => value.ToProtocolString();

    public static string ToProtocolString(this GraphicsMode value) {
        return value switch {
            GraphicsMode.Simple => "Simple",
            GraphicsMode.Fancy => "Fancy",
            GraphicsMode.Advanced => "Advanced",
            GraphicsMode.RayTraced => "RayTraced",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown GraphicsMode value.")
        };
    }

    public static GraphicsMode FromProtocolString(string value) {
        return value switch {
            "Simple" => GraphicsMode.Simple,
            "Fancy" => GraphicsMode.Fancy,
            "Advanced" => GraphicsMode.Advanced,
            "RayTraced" => GraphicsMode.RayTraced,
            _ => throw new ArgumentException($"Unknown GraphicsMode protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out GraphicsMode result) {
        switch (value) {
            case "Simple":
                result = GraphicsMode.Simple;
                return true;
            case "Fancy":
                result = GraphicsMode.Fancy;
                return true;
            case "Advanced":
                result = GraphicsMode.Advanced;
                return true;
            case "RayTraced":
                result = GraphicsMode.RayTraced;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
