#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum ItemUsePredictedResult {
    Failure = 0,
    Success = 1,
}

public static class ItemUsePredictedResultExtensions {
    public static string ToProtoString(this ItemUsePredictedResult value) => value.ToProtocolString();

    public static string ToProtocolString(this ItemUsePredictedResult value) {
        return value switch {
            ItemUsePredictedResult.Failure => "Failure",
            ItemUsePredictedResult.Success => "Success",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown ItemUsePredictedResult value.")
        };
    }

    public static ItemUsePredictedResult FromProtocolString(string value) {
        return value switch {
            "Failure" => ItemUsePredictedResult.Failure,
            "Success" => ItemUsePredictedResult.Success,
            _ => throw new ArgumentException($"Unknown ItemUsePredictedResult protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out ItemUsePredictedResult result) {
        switch (value) {
            case "Failure":
                result = ItemUsePredictedResult.Failure;
                return true;
            case "Success":
                result = ItemUsePredictedResult.Success;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
