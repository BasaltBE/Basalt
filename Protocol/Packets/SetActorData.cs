using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.SetActorData)]
public sealed record SetActorDataPacket : DataPacket
{
    /// <summary>
    /// Runtime id of the actor.
    /// </summary>
    public ulong RuntimeId;

    /// <summary>
    /// Metadata entries to apply.
    /// </summary>
    public List<ActorMetadataItem> Metadata = [];

    /// <summary>
    /// Server tick for this update.
    /// </summary>
    public ulong Tick;

    public override void Deserialize(Binary.BinaryReader reader)
    {
        RuntimeId = reader.ReadVarULong();

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

        Tick = reader.ReadVarULong();
    }

    public override void Serialize(Binary.BinaryWriter writer)
    {
        writer.WriteVarULong(RuntimeId);
        writer.WriteVarInt(Metadata.Count);
        for (int i = 0; i < Metadata.Count; i++)
        {
            Metadata[i].Write(writer);
        }

        writer.WriteVarInt(0);
        writer.WriteVarInt(0);
        writer.WriteVarULong(Tick);
    }
}
