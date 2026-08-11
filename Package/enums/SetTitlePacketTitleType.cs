using System;

namespace BedrockProtocol.Enums;

public enum SetTitlePacketTitleType {
    Clear = 0,
    Reset = 1,
    Title = 2,
    Subtitle = 3,
    Actionbar = 4,
    Times = 5,
    TitleTextObject = 6,
    SubtitleTextObject = 7,
    ActionbarTextObject = 8,
}

public static class SetTitlePacketTitleTypeExtensions {
    public static string ToProtoString(this SetTitlePacketTitleType value) => value.ToProtocolString();

    public static string ToProtocolString(this SetTitlePacketTitleType value) {
        return value switch {
            SetTitlePacketTitleType.Clear => "Clear",
            SetTitlePacketTitleType.Reset => "Reset",
            SetTitlePacketTitleType.Title => "Title",
            SetTitlePacketTitleType.Subtitle => "Subtitle",
            SetTitlePacketTitleType.Actionbar => "Actionbar",
            SetTitlePacketTitleType.Times => "Times",
            SetTitlePacketTitleType.TitleTextObject => "TitleTextObject",
            SetTitlePacketTitleType.SubtitleTextObject => "SubtitleTextObject",
            SetTitlePacketTitleType.ActionbarTextObject => "ActionbarTextObject",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown SetTitlePacketTitleType value.")
        };
    }

    public static SetTitlePacketTitleType FromProtocolString(string value) {
        return value switch {
            "Clear" => SetTitlePacketTitleType.Clear,
            "Reset" => SetTitlePacketTitleType.Reset,
            "Title" => SetTitlePacketTitleType.Title,
            "Subtitle" => SetTitlePacketTitleType.Subtitle,
            "Actionbar" => SetTitlePacketTitleType.Actionbar,
            "Times" => SetTitlePacketTitleType.Times,
            "TitleTextObject" => SetTitlePacketTitleType.TitleTextObject,
            "SubtitleTextObject" => SetTitlePacketTitleType.SubtitleTextObject,
            "ActionbarTextObject" => SetTitlePacketTitleType.ActionbarTextObject,
            _ => throw new ArgumentException($"Unknown SetTitlePacketTitleType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out SetTitlePacketTitleType result) {
        switch (value) {
            case "Clear":
                result = SetTitlePacketTitleType.Clear;
                return true;
            case "Reset":
                result = SetTitlePacketTitleType.Reset;
                return true;
            case "Title":
                result = SetTitlePacketTitleType.Title;
                return true;
            case "Subtitle":
                result = SetTitlePacketTitleType.Subtitle;
                return true;
            case "Actionbar":
                result = SetTitlePacketTitleType.Actionbar;
                return true;
            case "Times":
                result = SetTitlePacketTitleType.Times;
                return true;
            case "TitleTextObject":
                result = SetTitlePacketTitleType.TitleTextObject;
                return true;
            case "SubtitleTextObject":
                result = SetTitlePacketTitleType.SubtitleTextObject;
                return true;
            case "ActionbarTextObject":
                result = SetTitlePacketTitleType.ActionbarTextObject;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
