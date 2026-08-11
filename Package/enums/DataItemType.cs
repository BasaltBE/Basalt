using System;

namespace BedrockProtocol.Enums;

public enum DataItemType {
    Byte = 0,
    Short = 1,
    Int = 2,
    Float = 3,
    String = 4,
    CompoundTag = 5,
    Pos = 6,
    Int64 = 7,
    Vec3 = 8,
}

public static class DataItemTypeExtensions {
    public static string ToProtoString(this DataItemType value) => value.ToProtocolString();

    public static string ToProtocolString(this DataItemType value) {
        return value switch {
            DataItemType.Byte => "Byte",
            DataItemType.Short => "Short",
            DataItemType.Int => "Int",
            DataItemType.Float => "Float",
            DataItemType.String => "String",
            DataItemType.CompoundTag => "CompoundTag",
            DataItemType.Pos => "Pos",
            DataItemType.Int64 => "Int64",
            DataItemType.Vec3 => "Vec3",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown DataItemType value.")
        };
    }

    public static DataItemType FromProtocolString(string value) {
        return value switch {
            "Byte" => DataItemType.Byte,
            "Short" => DataItemType.Short,
            "Int" => DataItemType.Int,
            "Float" => DataItemType.Float,
            "String" => DataItemType.String,
            "CompoundTag" => DataItemType.CompoundTag,
            "Pos" => DataItemType.Pos,
            "Int64" => DataItemType.Int64,
            "Vec3" => DataItemType.Vec3,
            _ => throw new ArgumentException($"Unknown DataItemType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out DataItemType result) {
        switch (value) {
            case "Byte":
                result = DataItemType.Byte;
                return true;
            case "Short":
                result = DataItemType.Short;
                return true;
            case "Int":
                result = DataItemType.Int;
                return true;
            case "Float":
                result = DataItemType.Float;
                return true;
            case "String":
                result = DataItemType.String;
                return true;
            case "CompoundTag":
                result = DataItemType.CompoundTag;
                return true;
            case "Pos":
                result = DataItemType.Pos;
                return true;
            case "Int64":
                result = DataItemType.Int64;
                return true;
            case "Vec3":
                result = DataItemType.Vec3;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
