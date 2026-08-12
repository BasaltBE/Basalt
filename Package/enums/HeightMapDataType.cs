#nullable enable

using System;

namespace BedrockProtocol.Enums;

public enum HeightMapDataType {
    NoData = 0,
    HasData = 1,
    AllTooHigh = 2,
    AllTooLow = 3,
    AllCopied = 4,
}

public static class HeightMapDataTypeExtensions {
    public static string ToProtoString(this HeightMapDataType value) => value.ToProtocolString();

    public static string ToProtocolString(this HeightMapDataType value) {
        return value switch {
            HeightMapDataType.NoData => "NoData",
            HeightMapDataType.HasData => "HasData",
            HeightMapDataType.AllTooHigh => "AllTooHigh",
            HeightMapDataType.AllTooLow => "AllTooLow",
            HeightMapDataType.AllCopied => "AllCopied",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown HeightMapDataType value.")
        };
    }

    public static HeightMapDataType FromProtocolString(string value) {
        return value switch {
            "NoData" => HeightMapDataType.NoData,
            "HasData" => HeightMapDataType.HasData,
            "AllTooHigh" => HeightMapDataType.AllTooHigh,
            "AllTooLow" => HeightMapDataType.AllTooLow,
            "AllCopied" => HeightMapDataType.AllCopied,
            _ => throw new ArgumentException($"Unknown HeightMapDataType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out HeightMapDataType result) {
        switch (value) {
            case "NoData":
                result = HeightMapDataType.NoData;
                return true;
            case "HasData":
                result = HeightMapDataType.HasData;
                return true;
            case "AllTooHigh":
                result = HeightMapDataType.AllTooHigh;
                return true;
            case "AllTooLow":
                result = HeightMapDataType.AllTooLow;
                return true;
            case "AllCopied":
                result = HeightMapDataType.AllCopied;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
