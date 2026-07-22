using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ServerToClientHandshake)]
public sealed record ServerToClientHandshakePacket : DataPacket {
    public byte[] Jwt = [];

    public override void Serialize(Binary.BinaryWriter writer) {
        writer.WriteVarUInt((uint)Jwt.Length);
        writer.WriteBytes(Jwt);
    }

    public override void Deserialize(Binary.BinaryReader reader) {
        int length = checked((int)reader.ReadVarUInt());
        Jwt = reader.ReadBytes(length).ToArray();
    }
}
