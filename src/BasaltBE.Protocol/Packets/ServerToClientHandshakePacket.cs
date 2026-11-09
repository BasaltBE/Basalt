using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(3)]
public sealed class ServerToClientHandshakePacket : DataPacket {
    public string HandshakeWebToken = string.Empty;

    public override void Serialize(ref BinaryWriter writer) => writer.WriteVarString(HandshakeWebToken);

    public override void Deserialize(ref BinaryReader reader) => HandshakeWebToken = reader.ReadVarString();
}
