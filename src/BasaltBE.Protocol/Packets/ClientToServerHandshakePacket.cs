using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(4)]
public sealed class ClientToServerHandshakePacket : DataPacket {
    public override void Serialize(ref BinaryWriter writer) { }

    public override void Deserialize(ref BinaryReader reader) { }
}
