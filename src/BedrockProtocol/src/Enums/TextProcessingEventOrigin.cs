namespace Basalt.BedrockProtocol.Enums;

public enum TextProcessingEventOrigin {
    Unknown = -1,
    ServerChatPublic,
    ServerChatWhisper,
    SignText,
    AnvilText,
    BookAndQuillText,
    CommandBlockText,
    BlockActorDataText,
    JoinEventText,
    LeaveEventText,
    SlashCommandChat,
    CartographyText,
    KickCommand,
    TitleCommand,
    SummonCommand,
    ServerForm,
    DataDrivenUI
}
