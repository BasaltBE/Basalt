#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum CodeBuilderStorageQueryOptionsCategory {
    None = 0,
    CodeStatus = 1,
    Instantiation = 2,
}

public static class CodeBuilderStorageQueryOptionsCategoryExtensions {
    public static string ToProtoString(this CodeBuilderStorageQueryOptionsCategory value) => value.ToProtocolString();

    public static string ToProtocolString(this CodeBuilderStorageQueryOptionsCategory value) {
        return value switch {
            CodeBuilderStorageQueryOptionsCategory.None => "None",
            CodeBuilderStorageQueryOptionsCategory.CodeStatus => "CodeStatus",
            CodeBuilderStorageQueryOptionsCategory.Instantiation => "Instantiation",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown CodeBuilderStorageQueryOptionsCategory value.")
        };
    }

    public static CodeBuilderStorageQueryOptionsCategory FromProtocolString(string value) {
        return value switch {
            "None" => CodeBuilderStorageQueryOptionsCategory.None,
            "CodeStatus" => CodeBuilderStorageQueryOptionsCategory.CodeStatus,
            "Instantiation" => CodeBuilderStorageQueryOptionsCategory.Instantiation,
            _ => throw new ArgumentException($"Unknown CodeBuilderStorageQueryOptionsCategory protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out CodeBuilderStorageQueryOptionsCategory result) {
        switch (value) {
            case "None":
                result = CodeBuilderStorageQueryOptionsCategory.None;
                return true;
            case "CodeStatus":
                result = CodeBuilderStorageQueryOptionsCategory.CodeStatus;
                return true;
            case "Instantiation":
                result = CodeBuilderStorageQueryOptionsCategory.Instantiation;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
