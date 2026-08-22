using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class TextPacketBody : DataType {
    public TextPacketType MessageType;
    public string Message = string.Empty;
    public string PlayerName = string.Empty;
    public string[] Parameters = Array.Empty<string>();

    public override void Write(ref BinaryWriter writer) {
        writer.WriteUInt8(GetCategoryType(MessageType));
        writer.WriteUInt8((byte)MessageType);

        if (MessageType is TextPacketType.Raw or TextPacketType.Tip or TextPacketType.SystemMessage or TextPacketType.TextObjectWhisper or TextPacketType.TextObjectAnnouncement or TextPacketType.TextObject) {
            writer.WriteVarString(Message);
        } else if (MessageType is TextPacketType.Chat or TextPacketType.Whisper or TextPacketType.Announcement) {
            writer.WriteVarString(PlayerName);
            writer.WriteVarString(Message);
        } else if (MessageType is TextPacketType.Translate or TextPacketType.Popup or TextPacketType.JukeboxPopup) {
            writer.WriteVarString(Message);
            writer.WriteVarUInt((uint)Parameters.Length);

            foreach (string parameter in Parameters) {
                writer.WriteVarString(parameter);
            }
        } else {
            throw new ArgumentOutOfRangeException(nameof(MessageType));
        }
    }

    public override void Read(ref BinaryReader reader) {
        reader.ReadUInt8();
        MessageType = (TextPacketType)reader.ReadUInt8();

        if (MessageType is TextPacketType.Raw or TextPacketType.Tip or TextPacketType.SystemMessage or TextPacketType.TextObjectWhisper or TextPacketType.TextObjectAnnouncement or TextPacketType.TextObject) {
            Message = reader.ReadVarString();
        } else if (MessageType is TextPacketType.Chat or TextPacketType.Whisper or TextPacketType.Announcement) {
            PlayerName = reader.ReadVarString();
            Message = reader.ReadVarString();
        } else if (MessageType is TextPacketType.Translate or TextPacketType.Popup or TextPacketType.JukeboxPopup) {
            Message = reader.ReadVarString();
            int count = checked((int)reader.ReadVarUInt());
            Parameters = new string[count];

            for (int index = 0; index < count; index++) {
                Parameters[index] = reader.ReadVarString();
            }
        } else {
            throw new FormatException("Unsupported text packet type.");
        }
    }

    private static byte GetCategoryType(TextPacketType messageType) => messageType switch {
        TextPacketType.Raw or TextPacketType.Tip or TextPacketType.SystemMessage or TextPacketType.TextObjectWhisper or TextPacketType.TextObjectAnnouncement or TextPacketType.TextObject => 0,
        TextPacketType.Chat or TextPacketType.Whisper or TextPacketType.Announcement => 1,
        _ => 2,
    };
}
