using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(83)]
public sealed class ResourcePackChunkDataPacket : DataPacket {
    public string ResourceName = string.Empty;
    public uint ChunkId;
    public ulong ByteOffset;
    public ReadOnlyMemory<byte> ChunkData;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarString(ResourceName);
        writer.WriteUInt32(ChunkId, true);
        writer.WriteUInt64(ByteOffset, true);
        writer.WriteVarUInt((uint)ChunkData.Length);
        writer.WriteBytes(ChunkData.Span);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ResourceName = reader.ReadVarString();
        ChunkId = reader.ReadUInt32(true);
        ByteOffset = reader.ReadUInt64(true);
        uint length = reader.ReadVarUInt();
        ChunkData = reader.ReadBytes((int)length).ToArray();
    }
}
