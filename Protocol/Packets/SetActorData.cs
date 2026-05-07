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

    public override void Deserialize(ref BinaryReader reader)
    {
        RuntimeId = reader.ReadVarULong();

        int metadataCount = checked((int)reader.ReadVarUInt());
        Metadata = new List<ActorMetadataItem>(metadataCount);
        for (int i = 0; i < metadataCount; i++)
        {
            ActorMetadataItem item = new();
            item.Read(ref reader);
            Metadata.Add(item);
        }

        int intPropertyCount = checked((int)reader.ReadVarUInt());
        for (int i = 0; i < intPropertyCount; i++)
        {
            _ = reader.ReadVarInt();
            _ = reader.ReadVarInt();
        }

        int floatPropertyCount = checked((int)reader.ReadVarUInt());
        for (int i = 0; i < floatPropertyCount; i++)
        {
            _ = reader.ReadVarInt();
            _ = reader.ReadF32(true);
        }

        Tick = reader.ReadVarULong();
    }

    public override void Serialize(ref BinaryWriter writer)
    {
        writer.WriteVarULong(RuntimeId);
        writer.WriteVarUInt((uint)Metadata.Count);
        for (int i = 0; i < Metadata.Count; i++)
        {
            Metadata[i].Write(ref writer);
        }

        writer.WriteVarUInt(0);
        writer.WriteVarUInt(0);
        writer.WriteVarULong(Tick);
    }
}
