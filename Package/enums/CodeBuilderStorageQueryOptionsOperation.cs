#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum CodeBuilderStorageQueryOptionsOperation {
    None = 0,
    Get = 1,
    Set = 2,
    Reset = 3,
}

public static class CodeBuilderStorageQueryOptionsOperationExtensions {
    public static string ToProtoString(this CodeBuilderStorageQueryOptionsOperation value) => value.ToProtocolString();

    public static string ToProtocolString(this CodeBuilderStorageQueryOptionsOperation value) {
        return value switch {
            CodeBuilderStorageQueryOptionsOperation.None => "None",
            CodeBuilderStorageQueryOptionsOperation.Get => "Get",
            CodeBuilderStorageQueryOptionsOperation.Set => "Set",
            CodeBuilderStorageQueryOptionsOperation.Reset => "Reset",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown CodeBuilderStorageQueryOptionsOperation value.")
        };
    }

    public static CodeBuilderStorageQueryOptionsOperation FromProtocolString(string value) {
        return value switch {
            "None" => CodeBuilderStorageQueryOptionsOperation.None,
            "Get" => CodeBuilderStorageQueryOptionsOperation.Get,
            "Set" => CodeBuilderStorageQueryOptionsOperation.Set,
            "Reset" => CodeBuilderStorageQueryOptionsOperation.Reset,
            _ => throw new ArgumentException($"Unknown CodeBuilderStorageQueryOptionsOperation protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out CodeBuilderStorageQueryOptionsOperation result) {
        switch (value) {
            case "None":
                result = CodeBuilderStorageQueryOptionsOperation.None;
                return true;
            case "Get":
                result = CodeBuilderStorageQueryOptionsOperation.Get;
                return true;
            case "Set":
                result = CodeBuilderStorageQueryOptionsOperation.Set;
                return true;
            case "Reset":
                result = CodeBuilderStorageQueryOptionsOperation.Reset;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
