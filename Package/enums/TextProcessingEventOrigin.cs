using System;

namespace BedrockProtocol.Enums;

public enum TextProcessingEventOrigin {
    unknown = -1,
    ServerChatPublic = 0,
    ServerChatWhisper = 1,
    SignText = 2,
    AnvilText = 3,
    BookAndQuillText = 4,
    CommandBlockText = 5,
    BlockActorDataText = 6,
    JoinEventText = 7,
    LeaveEventText = 8,
    SlashCommandChat = 9,
    CartographyText = 10,
    KickCommand = 11,
    TitleCommand = 12,
    SummonCommand = 13,
    ServerForm = 14,
    DataDrivenUI = 15,
}

public static class TextProcessingEventOriginExtensions {
    public static string ToProtoString(this TextProcessingEventOrigin value) => value.ToProtocolString();

    public static string ToProtocolString(this TextProcessingEventOrigin value) {
        return value switch {
            TextProcessingEventOrigin.unknown => "unknown",
            TextProcessingEventOrigin.ServerChatPublic => "ServerChatPublic",
            TextProcessingEventOrigin.ServerChatWhisper => "ServerChatWhisper",
            TextProcessingEventOrigin.SignText => "SignText",
            TextProcessingEventOrigin.AnvilText => "AnvilText",
            TextProcessingEventOrigin.BookAndQuillText => "BookAndQuillText",
            TextProcessingEventOrigin.CommandBlockText => "CommandBlockText",
            TextProcessingEventOrigin.BlockActorDataText => "BlockActorDataText",
            TextProcessingEventOrigin.JoinEventText => "JoinEventText",
            TextProcessingEventOrigin.LeaveEventText => "LeaveEventText",
            TextProcessingEventOrigin.SlashCommandChat => "SlashCommandChat",
            TextProcessingEventOrigin.CartographyText => "CartographyText",
            TextProcessingEventOrigin.KickCommand => "KickCommand",
            TextProcessingEventOrigin.TitleCommand => "TitleCommand",
            TextProcessingEventOrigin.SummonCommand => "SummonCommand",
            TextProcessingEventOrigin.ServerForm => "ServerForm",
            TextProcessingEventOrigin.DataDrivenUI => "DataDrivenUI",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown TextProcessingEventOrigin value.")
        };
    }

    public static TextProcessingEventOrigin FromProtocolString(string value) {
        return value switch {
            "unknown" => TextProcessingEventOrigin.unknown,
            "ServerChatPublic" => TextProcessingEventOrigin.ServerChatPublic,
            "ServerChatWhisper" => TextProcessingEventOrigin.ServerChatWhisper,
            "SignText" => TextProcessingEventOrigin.SignText,
            "AnvilText" => TextProcessingEventOrigin.AnvilText,
            "BookAndQuillText" => TextProcessingEventOrigin.BookAndQuillText,
            "CommandBlockText" => TextProcessingEventOrigin.CommandBlockText,
            "BlockActorDataText" => TextProcessingEventOrigin.BlockActorDataText,
            "JoinEventText" => TextProcessingEventOrigin.JoinEventText,
            "LeaveEventText" => TextProcessingEventOrigin.LeaveEventText,
            "SlashCommandChat" => TextProcessingEventOrigin.SlashCommandChat,
            "CartographyText" => TextProcessingEventOrigin.CartographyText,
            "KickCommand" => TextProcessingEventOrigin.KickCommand,
            "TitleCommand" => TextProcessingEventOrigin.TitleCommand,
            "SummonCommand" => TextProcessingEventOrigin.SummonCommand,
            "ServerForm" => TextProcessingEventOrigin.ServerForm,
            "DataDrivenUI" => TextProcessingEventOrigin.DataDrivenUI,
            _ => throw new ArgumentException($"Unknown TextProcessingEventOrigin protocol value '{value}'.", nameof(value))
        };
    }

    public static bool TryFromProtocolString(string value, out TextProcessingEventOrigin result) {
        switch (value) {
            case "unknown":
                result = TextProcessingEventOrigin.unknown;
                return true;
            case "ServerChatPublic":
                result = TextProcessingEventOrigin.ServerChatPublic;
                return true;
            case "ServerChatWhisper":
                result = TextProcessingEventOrigin.ServerChatWhisper;
                return true;
            case "SignText":
                result = TextProcessingEventOrigin.SignText;
                return true;
            case "AnvilText":
                result = TextProcessingEventOrigin.AnvilText;
                return true;
            case "BookAndQuillText":
                result = TextProcessingEventOrigin.BookAndQuillText;
                return true;
            case "CommandBlockText":
                result = TextProcessingEventOrigin.CommandBlockText;
                return true;
            case "BlockActorDataText":
                result = TextProcessingEventOrigin.BlockActorDataText;
                return true;
            case "JoinEventText":
                result = TextProcessingEventOrigin.JoinEventText;
                return true;
            case "LeaveEventText":
                result = TextProcessingEventOrigin.LeaveEventText;
                return true;
            case "SlashCommandChat":
                result = TextProcessingEventOrigin.SlashCommandChat;
                return true;
            case "CartographyText":
                result = TextProcessingEventOrigin.CartographyText;
                return true;
            case "KickCommand":
                result = TextProcessingEventOrigin.KickCommand;
                return true;
            case "TitleCommand":
                result = TextProcessingEventOrigin.TitleCommand;
                return true;
            case "SummonCommand":
                result = TextProcessingEventOrigin.SummonCommand;
                return true;
            case "ServerForm":
                result = TextProcessingEventOrigin.ServerForm;
                return true;
            case "DataDrivenUI":
                result = TextProcessingEventOrigin.DataDrivenUI;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
