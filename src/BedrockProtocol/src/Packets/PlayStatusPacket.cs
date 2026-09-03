using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(2)]
public sealed class PlayStatusPacket : DataPacket {
    public PlayStatus Status;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteInt32((int)Status, false);
    }

    public override void Deserialize(ref BinaryReader reader) {
        Status = (PlayStatus)reader.ReadInt32(false);
    }
}
