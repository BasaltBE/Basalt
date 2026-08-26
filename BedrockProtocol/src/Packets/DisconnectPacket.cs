using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(5)]
public sealed class DisconnectPacket : DataPacket {
    public DisconnectFailReason Reason;
    public DisconnectPacketMessages? Messages;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteZigZag((int)Reason);
        writer.WriteBool(Messages is null);

        if (Messages is not null) {
            Messages.Write(ref writer);
        }
    }

    public override void Deserialize(ref BinaryReader reader) {
        Reason = (DisconnectFailReason)reader.ReadZigZag();

        if (!reader.ReadBool()) {
            Messages = new();
            Messages.Read(ref reader);
        }
        else {
            Messages = null;
        }
    }
}
