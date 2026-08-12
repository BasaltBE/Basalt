#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum CodeBuilderExecutionStateCodeStatus {
    None = 0,
    NotStarted = 1,
    InProgress = 2,
    Paused = 3,
    Error = 4,
    Succeeded = 5,
}

public static class CodeBuilderExecutionStateCodeStatusExtensions {
    public static string ToProtoString(this CodeBuilderExecutionStateCodeStatus value) => value.ToProtocolString();

    public static string ToProtocolString(this CodeBuilderExecutionStateCodeStatus value) {
        return value switch {
            CodeBuilderExecutionStateCodeStatus.None => "None",
            CodeBuilderExecutionStateCodeStatus.NotStarted => "NotStarted",
            CodeBuilderExecutionStateCodeStatus.InProgress => "InProgress",
            CodeBuilderExecutionStateCodeStatus.Paused => "Paused",
            CodeBuilderExecutionStateCodeStatus.Error => "Error",
            CodeBuilderExecutionStateCodeStatus.Succeeded => "Succeeded",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown CodeBuilderExecutionStateCodeStatus value.")
        };
    }

    public static CodeBuilderExecutionStateCodeStatus FromProtocolString(string value) {
        return value switch {
            "None" => CodeBuilderExecutionStateCodeStatus.None,
            "NotStarted" => CodeBuilderExecutionStateCodeStatus.NotStarted,
            "InProgress" => CodeBuilderExecutionStateCodeStatus.InProgress,
            "Paused" => CodeBuilderExecutionStateCodeStatus.Paused,
            "Error" => CodeBuilderExecutionStateCodeStatus.Error,
            "Succeeded" => CodeBuilderExecutionStateCodeStatus.Succeeded,
            _ => throw new ArgumentException($"Unknown CodeBuilderExecutionStateCodeStatus protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out CodeBuilderExecutionStateCodeStatus result) {
        switch (value) {
            case "None":
                result = CodeBuilderExecutionStateCodeStatus.None;
                return true;
            case "NotStarted":
                result = CodeBuilderExecutionStateCodeStatus.NotStarted;
                return true;
            case "InProgress":
                result = CodeBuilderExecutionStateCodeStatus.InProgress;
                return true;
            case "Paused":
                result = CodeBuilderExecutionStateCodeStatus.Paused;
                return true;
            case "Error":
                result = CodeBuilderExecutionStateCodeStatus.Error;
                return true;
            case "Succeeded":
                result = CodeBuilderExecutionStateCodeStatus.Succeeded;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
