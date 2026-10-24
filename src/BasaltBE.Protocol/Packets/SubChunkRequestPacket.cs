using Basalt.Binary;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Packets;

[PacketId(175)]
public sealed class SubChunkRequestPacket : DataPacket {
    public int DimensionId;
    public SubChunkPosOffset[] PositionOffsets = [];
    public SubChunkPos CenterPosition = new();

    public override void Deserialize(ref BinaryReader reader) {
        DimensionId = reader.ReadZigZag();
        uint count = reader.ReadVarUInt();
        if (count > 4096 || count > reader.Remaining / 3)
            throw new InvalidDataException("Subchunk request count is invalid.");

        PositionOffsets = new SubChunkPosOffset[(int)count];
        for (int i = 0; i < PositionOffsets.Length; i++)  {
            SubChunkPosOffset offset = new();
            offset.Read(ref reader);
            PositionOffsets[i] = offset;
        }

        CenterPosition.Read(ref reader);
    }

    public override void Serialize(ref BinaryWriter writer) {
        ArgumentNullException.ThrowIfNull(CenterPosition);
        if (PositionOffsets.Length > 4096)
            throw new InvalidDataException("Subchunk request count cannot exceed 4096.");

        writer.WriteZigZag(DimensionId);
        writer.WriteVarUInt((uint)PositionOffsets.Length);

        foreach (SubChunkPosOffset offset in PositionOffsets) {
            ArgumentNullException.ThrowIfNull(offset);
            offset.Write(ref writer);
        }
        CenterPosition.Write(ref writer);
    }
}
