using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record SetActorDataPacket : DataPacket
{
    public ulong RuntimeId { get; set; }
    public List<ActorMetadataItem> Metadata { get; set; } = [];
    public ulong Tick { get; set; }

    public override PacketId PacketId => PacketId.SetActorData;

    public override void Deserialize(BinaryReader reader)
    {
        RuntimeId = unchecked((ulong)reader.ReadVarLong());

        int metadataCount = reader.ReadVarInt();
        Metadata = new List<ActorMetadataItem>(metadataCount);
        for (int i = 0; i < metadataCount; i++)
        {
            ActorMetadataItem item = new();
            item.Read(reader);
            Metadata.Add(item);
        }

        int intPropertyCount = reader.ReadVarInt();
        for (int i = 0; i < intPropertyCount; i++)
        {
            _ = reader.ReadVarInt();
            _ = reader.ReadZigZag();
        }

        int floatPropertyCount = reader.ReadVarInt();
        for (int i = 0; i < floatPropertyCount; i++)
        {
            _ = reader.ReadVarInt();
            _ = reader.ReadF32(true);
        }

        Tick = unchecked((ulong)reader.ReadVarLong());
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarLong(unchecked((long)RuntimeId));
        writer.WriteVarInt(Metadata.Count);
        for (int i = 0; i < Metadata.Count; i++)
        {
            Metadata[i].Write(writer);
        }

        writer.WriteVarInt(0);
        writer.WriteVarInt(0);
        writer.WriteVarLong(unchecked((long)Tick));
    }
}
