using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ResourcePackChunkData)]
public sealed record ResourcePackChunkDataPacket : DataPacket
{
    public string Uuid = string.Empty;
    public uint ChunkIndex;
    public ulong DataOffset;
    public byte[] Data = [];

    public override void Deserialize(BinaryReader reader)
    {
        Uuid = reader.ReadVarString();
        ChunkIndex = reader.ReadUInt32(true);
        DataOffset = reader.ReadUInt64(true);
        int length = checked((int)reader.ReadVarUInt());
        Data = reader.ReadBytes(length).ToArray();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarString(Uuid);
        writer.WriteUInt32(ChunkIndex, true);
        writer.WriteUInt64(DataOffset, true);
        writer.WriteVarUInt((uint)Data.Length);
        writer.WriteBytes(Data);
    }
}
