using System;

namespace BedrockProtocol.Enums;

public enum TextPacketType {
    raw = 0,
    chat = 1,
    translate = 2,
    popup = 3,
    jukeboxPopup = 4,
    tip = 5,
    systemMessage = 6,
    whisper = 7,
    announcement = 8,
    textObjectWhisper = 9,
    textObject = 10,
    textObjectAnnouncement = 11,
}

public static class TextPacketTypeExtensions {
    public static string ToProtoString(this TextPacketType value) => value.ToProtocolString();

    public static string ToProtocolString(this TextPacketType value) {
        return value switch {
            TextPacketType.raw => "raw",
            TextPacketType.chat => "chat",
            TextPacketType.translate => "translate",
            TextPacketType.popup => "popup",
            TextPacketType.jukeboxPopup => "jukeboxPopup",
            TextPacketType.tip => "tip",
            TextPacketType.systemMessage => "systemMessage",
            TextPacketType.whisper => "whisper",
            TextPacketType.announcement => "announcement",
            TextPacketType.textObjectWhisper => "textObjectWhisper",
            TextPacketType.textObject => "textObject",
            TextPacketType.textObjectAnnouncement => "textObjectAnnouncement",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown TextPacketType value.")
        };
    }

    public static TextPacketType FromProtocolString(string value) {
        return value switch {
            "raw" => TextPacketType.raw,
            "chat" => TextPacketType.chat,
            "translate" => TextPacketType.translate,
            "popup" => TextPacketType.popup,
            "jukeboxPopup" => TextPacketType.jukeboxPopup,
            "tip" => TextPacketType.tip,
            "systemMessage" => TextPacketType.systemMessage,
            "whisper" => TextPacketType.whisper,
            "announcement" => TextPacketType.announcement,
            "textObjectWhisper" => TextPacketType.textObjectWhisper,
            "textObject" => TextPacketType.textObject,
            "textObjectAnnouncement" => TextPacketType.textObjectAnnouncement,
            _ => throw new ArgumentException($"Unknown TextPacketType protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out TextPacketType result) {
        switch (value) {
            case "raw":
                result = TextPacketType.raw;
                return true;
            case "chat":
                result = TextPacketType.chat;
                return true;
            case "translate":
                result = TextPacketType.translate;
                return true;
            case "popup":
                result = TextPacketType.popup;
                return true;
            case "jukeboxPopup":
                result = TextPacketType.jukeboxPopup;
                return true;
            case "tip":
                result = TextPacketType.tip;
                return true;
            case "systemMessage":
                result = TextPacketType.systemMessage;
                return true;
            case "whisper":
                result = TextPacketType.whisper;
                return true;
            case "announcement":
                result = TextPacketType.announcement;
                return true;
            case "textObjectWhisper":
                result = TextPacketType.textObjectWhisper;
                return true;
            case "textObject":
                result = TextPacketType.textObject;
                return true;
            case "textObjectAnnouncement":
                result = TextPacketType.textObjectAnnouncement;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
