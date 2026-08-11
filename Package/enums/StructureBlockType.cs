using System;

namespace BedrockProtocol.Enums;

public enum StructureBlockType {
    Data = 0,
    Save = 1,
    Load = 2,
    Corner = 3,
    Invalid = 4,
    Export = 5,
}

public static class StructureBlockTypeExtensions {
    public static string ToProtoString(this StructureBlockType value) => value.ToProtocolString();

    public static string ToProtocolString(this StructureBlockType value) {
        return value switch {
            StructureBlockType.Data => "Data",
            StructureBlockType.Save => "Save",
            StructureBlockType.Load => "Load",
            StructureBlockType.Corner => "Corner",
            StructureBlockType.Invalid => "Invalid",
            StructureBlockType.Export => "Export",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown StructureBlockType value.")
        };
    }

    public static StructureBlockType FromProtocolString(string value) {
        return value switch {
            "Data" => StructureBlockType.Data,
            "Save" => StructureBlockType.Save,
            "Load" => StructureBlockType.Load,
            "Corner" => StructureBlockType.Corner,
            "Invalid" => StructureBlockType.Invalid,
            "Export" => StructureBlockType.Export,
            _ => throw new ArgumentException($"Unknown StructureBlockType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out StructureBlockType result) {
        switch (value) {
            case "Data":
                result = StructureBlockType.Data;
                return true;
            case "Save":
                result = StructureBlockType.Save;
                return true;
            case "Load":
                result = StructureBlockType.Load;
                return true;
            case "Corner":
                result = StructureBlockType.Corner;
                return true;
            case "Invalid":
                result = StructureBlockType.Invalid;
                return true;
            case "Export":
                result = StructureBlockType.Export;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
