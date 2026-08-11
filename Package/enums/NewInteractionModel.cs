using System;

namespace BedrockProtocol.Enums;

public enum NewInteractionModel {
    Touch = 0,
    Crosshair = 1,
    Classic = 2,
    Count = 3,
}

public static class NewInteractionModelExtensions {
    public static string ToProtoString(this NewInteractionModel value) => value.ToProtocolString();

    public static string ToProtocolString(this NewInteractionModel value) {
        return value switch {
            NewInteractionModel.Touch => "Touch",
            NewInteractionModel.Crosshair => "Crosshair",
            NewInteractionModel.Classic => "Classic",
            NewInteractionModel.Count => "Count",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown NewInteractionModel value.")
        };
    }

    public static NewInteractionModel FromProtocolString(string value) {
        return value switch {
            "Touch" => NewInteractionModel.Touch,
            "Crosshair" => NewInteractionModel.Crosshair,
            "Classic" => NewInteractionModel.Classic,
            "Count" => NewInteractionModel.Count,
            _ => throw new ArgumentException($"Unknown NewInteractionModel protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out NewInteractionModel result) {
        switch (value) {
            case "Touch":
                result = NewInteractionModel.Touch;
                return true;
            case "Crosshair":
                result = NewInteractionModel.Crosshair;
                return true;
            case "Classic":
                result = NewInteractionModel.Classic;
                return true;
            case "Count":
                result = NewInteractionModel.Count;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
