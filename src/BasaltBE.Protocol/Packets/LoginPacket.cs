using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(1)]
public sealed class LoginPacket : DataPacket {
    public int ClientNetworkVersion;
    public ReadOnlyMemory<byte> ConnectionRequest;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteInt32(ClientNetworkVersion, false);
        writer.WriteVarUInt((uint)ConnectionRequest.Length);
        writer.WriteBytes(ConnectionRequest.Span);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ClientNetworkVersion = reader.ReadInt32(false);
        uint length = reader.ReadVarUInt();
        ConnectionRequest = reader.ReadBytes((int)length).ToArray();
    }
}
