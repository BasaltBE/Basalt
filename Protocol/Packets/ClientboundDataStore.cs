using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ClientboundDataStore)]
public sealed record ClientboundDataStorePacket : DataPacket
{
    public List<DataStoreChangeInfo> Updates = [];

    public override void Deserialize(Binary.BinaryReader reader)
    {
        int count = reader.ReadVarInt();
        Updates = new List<DataStoreChangeInfo>(count);
        for (int i = 0; i < count; i++)
        {
            Updates.Add(DataStoreChangeInfoEntry.Read(reader));
        }
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarInt(Updates.Count);
        for (int i = 0; i < Updates.Count; i++)
        {
            DataStoreChangeInfoEntry.Write(writer, Updates[i]);
        }
    }
}
