using System;

namespace BedrockProtocol.Enums;

public enum CoordinateEvaluationOrder {
    XYZ = 0,
    XZY = 1,
    YXZ = 2,
    YZX = 3,
    ZXY = 4,
    ZYX = 5,
}

public static class CoordinateEvaluationOrderExtensions {
    public static string ToProtoString(this CoordinateEvaluationOrder value) => value.ToProtocolString();

    public static string ToProtocolString(this CoordinateEvaluationOrder value) {
        return value switch {
            CoordinateEvaluationOrder.XYZ => "XYZ",
            CoordinateEvaluationOrder.XZY => "XZY",
            CoordinateEvaluationOrder.YXZ => "YXZ",
            CoordinateEvaluationOrder.YZX => "YZX",
            CoordinateEvaluationOrder.ZXY => "ZXY",
            CoordinateEvaluationOrder.ZYX => "ZYX",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown CoordinateEvaluationOrder value.")
        };
    }

    public static CoordinateEvaluationOrder FromProtocolString(string value) {
        return value switch {
            "XYZ" => CoordinateEvaluationOrder.XYZ,
            "XZY" => CoordinateEvaluationOrder.XZY,
            "YXZ" => CoordinateEvaluationOrder.YXZ,
            "YZX" => CoordinateEvaluationOrder.YZX,
            "ZXY" => CoordinateEvaluationOrder.ZXY,
            "ZYX" => CoordinateEvaluationOrder.ZYX,
            _ => throw new ArgumentException($"Unknown CoordinateEvaluationOrder protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out CoordinateEvaluationOrder result) {
        switch (value) {
            case "XYZ":
                result = CoordinateEvaluationOrder.XYZ;
                return true;
            case "XZY":
                result = CoordinateEvaluationOrder.XZY;
                return true;
            case "YXZ":
                result = CoordinateEvaluationOrder.YXZ;
                return true;
            case "YZX":
                result = CoordinateEvaluationOrder.YZX;
                return true;
            case "ZXY":
                result = CoordinateEvaluationOrder.ZXY;
                return true;
            case "ZYX":
                result = CoordinateEvaluationOrder.ZYX;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
