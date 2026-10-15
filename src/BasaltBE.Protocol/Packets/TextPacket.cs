using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(9)]
public sealed class TextPacket : DataPacket {
    private const int MaximumPayloadLength = 4096;

    public bool Localize;
    public TextMessageType Type;
    public string SourceName = string.Empty;
    public string Message = string.Empty;
    public string[] Parameters = [];
    public string Xuid = string.Empty;
    public string PlatformId = string.Empty;
    public string? FilteredMessage;

    public override void Deserialize(ref BinaryReader reader) {
        if (reader.Remaining > MaximumPayloadLength)
            throw new InvalidDataException("Text packet exceeds the payload length limit.");

        Localize = reader.ReadBool();
        byte category = reader.ReadUInt8();
        Type = (TextMessageType)reader.ReadUInt8();
        byte expectedCategory = Type switch {
            TextMessageType.Raw or TextMessageType.Tip or TextMessageType.SystemMessage or
                TextMessageType.TextObjectWhisper or TextMessageType.TextObject or
                TextMessageType.TextObjectAnnouncement => 0,
            TextMessageType.Chat or TextMessageType.Whisper or TextMessageType.Announcement => 1,
            _ => 2
        };
        if (category != expectedCategory)
            throw new InvalidDataException($"Invalid text message category {category} for type {Type}.");
        switch (Type) {
            case TextMessageType.Raw:
            case TextMessageType.Tip:
            case TextMessageType.SystemMessage:
            case TextMessageType.TextObjectWhisper:
            case TextMessageType.TextObject:
            case TextMessageType.TextObjectAnnouncement:
                Message = reader.ReadVarString();
                break;
            case TextMessageType.Chat:
            case TextMessageType.Whisper:
            case TextMessageType.Announcement:
                SourceName = reader.ReadVarString();
                Message = reader.ReadVarString();
                break;
            case TextMessageType.Translate:
            case TextMessageType.Popup:
            case TextMessageType.JukeboxPopup:
                Message = reader.ReadVarString();
                uint parameterCount = reader.ReadVarUInt();
                if (parameterCount > reader.Remaining)
                    throw new InvalidDataException("Text parameter count exceeds the remaining payload.");
                Parameters = new string[(int)parameterCount];
                for (int index = 0; index < Parameters.Length; index++)
                    Parameters[index] = reader.ReadVarString();
                break;
            default:
                throw new InvalidDataException($"Unknown text message type {(byte)Type}.");
        }
        Xuid = reader.ReadVarString();
        PlatformId = reader.ReadVarString();
        FilteredMessage = reader.ReadBool() ? reader.ReadVarString() : null;
    }

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteBool(Localize);
        writer.WriteUInt8(Type switch {
            TextMessageType.Raw or TextMessageType.Tip or TextMessageType.SystemMessage or
                TextMessageType.TextObjectWhisper or TextMessageType.TextObject or
                TextMessageType.TextObjectAnnouncement => (byte)0,
            TextMessageType.Chat or TextMessageType.Whisper or TextMessageType.Announcement => (byte)1,
            _ => (byte)2
        });
        writer.WriteUInt8((byte)Type);
        switch (Type) {
            case TextMessageType.Raw:
            case TextMessageType.Tip:
            case TextMessageType.SystemMessage:
            case TextMessageType.TextObjectWhisper:
            case TextMessageType.TextObject:
            case TextMessageType.TextObjectAnnouncement:
                writer.WriteVarString(Message);
                break;
            case TextMessageType.Chat:
            case TextMessageType.Whisper:
            case TextMessageType.Announcement:
                writer.WriteVarString(SourceName);
                writer.WriteVarString(Message);
                break;
            case TextMessageType.Translate:
            case TextMessageType.Popup:
            case TextMessageType.JukeboxPopup:
                writer.WriteVarString(Message);
                writer.WriteVarUInt((uint)Parameters.Length);
                foreach (string parameter in Parameters)
                    writer.WriteVarString(parameter);
                break;
            default:
                throw new InvalidDataException($"Unknown text message type {(byte)Type}.");
        }
        writer.WriteVarString(Xuid);
        writer.WriteVarString(PlatformId);
        writer.WriteBool(FilteredMessage is not null);
        if (FilteredMessage is not null)
            writer.WriteVarString(FilteredMessage);
    }
}
