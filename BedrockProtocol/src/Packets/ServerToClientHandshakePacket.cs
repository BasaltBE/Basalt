using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(3)]
public sealed class ServerToClientHandshakePacket : DataPacket {
    public string Jwt = string.Empty;

    public override void Serialize(ref BinaryWriter writer) => writer.WriteVarString(Jwt);
    public override void Deserialize(ref BinaryReader reader) => Jwt = reader.ReadVarString();
}
