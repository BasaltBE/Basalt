using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

public sealed record RequestChunkRadiusPacket : DataPacket
{
    // Max chunk radius in the players settings
    public int ChunkRadius { get; set; }
    // Max chunk radius the client thinks is best depending on the device specs
    // Or reasonable rendering distance
    public byte MaxChunkRadius { get; set; }

    public override PacketId PacketId => PacketId.RequestChunkRadius;

    public override void Deserialize(BinaryReader reader)
    {
        ChunkRadius = reader.ReadVarInt();
        MaxChunkRadius = reader.ReadUInt8();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarInt(ChunkRadius);
        writer.WriteUInt8(MaxChunkRadius);
    }
}
