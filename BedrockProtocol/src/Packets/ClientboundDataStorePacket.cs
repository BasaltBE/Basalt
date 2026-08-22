using Basalt.BedrockProtocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(330)]
public sealed class ClientboundDataStorePacket : DataPacket {
    public ClientboundDataStoreUpdate[] Updates = [];

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarUInt((uint)Updates.Length);
        for (int i = 0; i < Updates.Length; i++) Updates[i].Write(ref writer);
    }

    public override void Deserialize(ref BinaryReader reader) {
        int count = checked((int)reader.ReadVarUInt());
        Updates = new ClientboundDataStoreUpdate[count];
        for (int i = 0; i < count; i++) {
            Updates[i] = new ClientboundDataStoreUpdate();
            Updates[i].Read(ref reader);
        }
    }
}
