using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(5)]
public sealed class DisconnectPacket : DataPacket {
    public DisconnectFailReason Reason;
    public bool HideDisconnectReason;
    public string Message = string.Empty;
    public string FilteredMessage = string.Empty;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarInt((int)Reason);
        writer.WriteBool(HideDisconnectReason);
        if (!HideDisconnectReason) {
            writer.WriteVarString(Message);
            writer.WriteVarString(FilteredMessage);
        }
    }

    public override void Deserialize(ref BinaryReader reader) {
        Reason = (DisconnectFailReason)reader.ReadVarInt();
        HideDisconnectReason = reader.ReadBool();
        if (!HideDisconnectReason) {
            Message = reader.ReadVarString();
            FilteredMessage = reader.ReadVarString();
        }
    }
}
