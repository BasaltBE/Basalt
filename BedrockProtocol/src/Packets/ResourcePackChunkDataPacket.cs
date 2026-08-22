using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Packets;

[PacketId(83)]
public sealed class ResourcePackChunkDataPacket : DataPacket {
    public string ResourceName = string.Empty;
    public uint ChunkId;
    public ulong ByteOffset;
    public string ChunkData = string.Empty;

    public override void Serialize(ref BinaryWriter writer) {
        writer.WriteVarString(ResourceName);
        writer.WriteUInt32(ChunkId, true);
        writer.WriteUInt64(ByteOffset, true);
        writer.WriteVarString(ChunkData);
    }

    public override void Deserialize(ref BinaryReader reader) {
        ResourceName = reader.ReadVarString();
        ChunkId = reader.ReadUInt32(true);
        ByteOffset = reader.ReadUInt64(true);
        ChunkData = reader.ReadVarString();
    }
}
