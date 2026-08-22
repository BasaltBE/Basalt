using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(5)]
public sealed class DisconnectPacket : DataPacket {
    public DisconnectFailReason Reason;
    public bool MessageSkipped;
    public DisconnectPacketMessages Messages = new();

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarInt((int)Reason);
        writer.WriteVarUInt(MessageSkipped ? 1u : 0u);

        if (!MessageSkipped) {
            Messages.Write(ref writer);
        }
    }

    public override void Deserialize(ref BinaryReader reader) {
        Reason = (DisconnectFailReason)reader.ReadVarInt();
        MessageSkipped = reader.ReadVarUInt() != 0;

        if (!MessageSkipped) {
            Messages.Read(ref reader);
        }
    }
}
