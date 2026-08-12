#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum ScriptPrimitiveShapeType {
    Line = 0,
    Box = 1,
    Sphere = 2,
    Circle = 3,
    Text = 4,
    Arrow = 5,
    Cylinder = 6,
    Pyramid = 7,
    Ellipsoid = 8,
    Cone = 9,
}

public static class ScriptPrimitiveShapeTypeExtensions {
    public static string ToProtoString(this ScriptPrimitiveShapeType value) => value.ToProtocolString();

    public static string ToProtocolString(this ScriptPrimitiveShapeType value) {
        return value switch {
            ScriptPrimitiveShapeType.Line => "Line",
            ScriptPrimitiveShapeType.Box => "Box",
            ScriptPrimitiveShapeType.Sphere => "Sphere",
            ScriptPrimitiveShapeType.Circle => "Circle",
            ScriptPrimitiveShapeType.Text => "Text",
            ScriptPrimitiveShapeType.Arrow => "Arrow",
            ScriptPrimitiveShapeType.Cylinder => "Cylinder",
            ScriptPrimitiveShapeType.Pyramid => "Pyramid",
            ScriptPrimitiveShapeType.Ellipsoid => "Ellipsoid",
            ScriptPrimitiveShapeType.Cone => "Cone",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ScriptPrimitiveShapeType value.")
        };
    }

    public static ScriptPrimitiveShapeType FromProtocolString(string value) {
        return value switch {
            "Line" => ScriptPrimitiveShapeType.Line,
            "Box" => ScriptPrimitiveShapeType.Box,
            "Sphere" => ScriptPrimitiveShapeType.Sphere,
            "Circle" => ScriptPrimitiveShapeType.Circle,
            "Text" => ScriptPrimitiveShapeType.Text,
            "Arrow" => ScriptPrimitiveShapeType.Arrow,
            "Cylinder" => ScriptPrimitiveShapeType.Cylinder,
            "Pyramid" => ScriptPrimitiveShapeType.Pyramid,
            "Ellipsoid" => ScriptPrimitiveShapeType.Ellipsoid,
            "Cone" => ScriptPrimitiveShapeType.Cone,
            _ => throw new ArgumentException($"Unknown ScriptPrimitiveShapeType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ScriptPrimitiveShapeType result) {
        switch (value) {
            case "Line":
                result = ScriptPrimitiveShapeType.Line;
                return true;
            case "Box":
                result = ScriptPrimitiveShapeType.Box;
                return true;
            case "Sphere":
                result = ScriptPrimitiveShapeType.Sphere;
                return true;
            case "Circle":
                result = ScriptPrimitiveShapeType.Circle;
                return true;
            case "Text":
                result = ScriptPrimitiveShapeType.Text;
                return true;
            case "Arrow":
                result = ScriptPrimitiveShapeType.Arrow;
                return true;
            case "Cylinder":
                result = ScriptPrimitiveShapeType.Cylinder;
                return true;
            case "Pyramid":
                result = ScriptPrimitiveShapeType.Pyramid;
                return true;
            case "Ellipsoid":
                result = ScriptPrimitiveShapeType.Ellipsoid;
                return true;
            case "Cone":
                result = ScriptPrimitiveShapeType.Cone;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
