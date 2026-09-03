using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(1)]
public sealed class LoginPacket : DataPacket {
    public int ClientNetworkVersion;
    public byte[] ConnectionRequest = [];

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteInt32(ClientNetworkVersion, false);
        writer.WriteVarUInt((uint)ConnectionRequest.Length);
        writer.WriteBytes(ConnectionRequest);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ClientNetworkVersion = reader.ReadInt32(false);
        int connectionRequestLength = checked((int)reader.ReadVarUInt());
        if (connectionRequestLength > reader.Remaining) {
            throw new InvalidDataException("Invalid login connection request length.");
        }

        ConnectionRequest = reader.ReadBytes(connectionRequestLength).ToArray();
    }
}
