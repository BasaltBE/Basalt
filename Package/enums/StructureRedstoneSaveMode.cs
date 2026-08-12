#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum StructureRedstoneSaveMode {
    SavesToMemory = 0,
    SavesToDisk = 1,
}

public static class StructureRedstoneSaveModeExtensions {
    public static string ToProtoString(this StructureRedstoneSaveMode value) => value.ToProtocolString();

    public static string ToProtocolString(this StructureRedstoneSaveMode value) {
        return value switch {
            StructureRedstoneSaveMode.SavesToMemory => "SavesToMemory",
            StructureRedstoneSaveMode.SavesToDisk => "SavesToDisk",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown StructureRedstoneSaveMode value.")
        };
    }

    public static StructureRedstoneSaveMode FromProtocolString(string value) {
        return value switch {
            "SavesToMemory" => StructureRedstoneSaveMode.SavesToMemory,
            "SavesToDisk" => StructureRedstoneSaveMode.SavesToDisk,
            _ => throw new ArgumentException($"Unknown StructureRedstoneSaveMode protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out StructureRedstoneSaveMode result) {
        switch (value) {
            case "SavesToMemory":
                result = StructureRedstoneSaveMode.SavesToMemory;
                return true;
            case "SavesToDisk":
                result = StructureRedstoneSaveMode.SavesToDisk;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
