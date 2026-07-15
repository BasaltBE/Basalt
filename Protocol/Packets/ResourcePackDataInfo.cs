using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Packets;

[Packet(PacketId.ResourcePackDataInfo)]
public sealed record ResourcePackDataInfoPacket : DataPacket
{
    public string Uuid = string.Empty;
    public uint ChunkSize;
    public uint ChunkCount;
    public ulong Size;
    public byte[] Hash = [];
    public bool Premium;
    public byte PackType;

    public override void Deserialize(BinaryReader reader)
    {
        Uuid = reader.ReadVarString();
        ChunkSize = reader.ReadUInt32(true);
        ChunkCount = reader.ReadUInt32(true);
        Size = reader.ReadUInt64(true);
        int hashLength = checked((int)reader.ReadVarUInt());
        Hash = reader.ReadBytes(hashLength).ToArray();
        Premium = reader.ReadBool();
        PackType = reader.ReadUInt8();
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.WriteVarString(Uuid);
        writer.WriteUInt32(ChunkSize, true);
        writer.WriteUInt32(ChunkCount, true);
        writer.WriteUInt64(Size, true);
        writer.WriteVarUInt((uint)Hash.Length);
        writer.WriteBytes(Hash);
        writer.WriteBool(Premium);
        writer.WriteUInt8(PackType);
    }
}
