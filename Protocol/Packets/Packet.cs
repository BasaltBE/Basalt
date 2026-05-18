using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public abstract record DataPacket
{
    public abstract PacketId PacketId { get; }

    public abstract void Deserialize(BinaryReader reader);
    public abstract void Serialize(BinaryWriter writer);

    public DataPacket Deserialize(ReadOnlySpan<byte> src)
    {
        int offset = 0;
        BinaryReader reader = new(src, ref offset);
        reader.ReadVarInt();
        Deserialize(reader);
        return this;
    }

    public ReadOnlySpan<byte> Serialize(Span<byte> dst)
    {
        int offset = 0;
        BinaryWriter writer = new(dst, ref offset);
        writer.WriteVarInt((byte)PacketId);
        Serialize(writer);
        return dst[writer.GetProcessedRange()];
    }
}
