using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(174)]
public sealed class SubChunkPacket : DataPacket {
    public bool CacheEnabled;
    public int DimensionId;
    public SubChunkPos CenterPosition = new();
    public SubChunkPacketData[] SubChunks = [];

    public override void Deserialize(ref BinaryReader reader) {
        CacheEnabled = reader.ReadBool();
        DimensionId = reader.ReadZigZag();
        CenterPosition.Read(ref reader);
        uint count = reader.ReadVarUInt();
        if (count > 4096 || count > reader.Remaining / 5)
            throw new InvalidDataException("Subchunk response count is invalid.");
        SubChunks = new SubChunkPacketData[(int)count];
        for (int i = 0; i < SubChunks.Length; i++) {
            SubChunkPacketData data = new();
            data.Read(ref reader, CacheEnabled);
            SubChunks[i] = data;
        }
    }

    public override void Serialize(ref BinaryWriter writer) {
        ArgumentNullException.ThrowIfNull(CenterPosition);
        if (SubChunks.Length > 4096)
            throw new InvalidDataException("Subchunk response count cannot exceed 4096.");
        writer.WriteBool(CacheEnabled);
        writer.WriteZigZag(DimensionId);
        CenterPosition.Write(ref writer);
        writer.WriteVarUInt((uint)SubChunks.Length);
        foreach (SubChunkPacketData data in SubChunks) {
            ArgumentNullException.ThrowIfNull(data);
            data.Write(ref writer, CacheEnabled);
        }
    }
}
