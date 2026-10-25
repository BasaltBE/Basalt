using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Protocol.Types;

public sealed class SubChunkHeightmapData : DataType {
    public SubChunkHeightmapType HeightmapType;
    public sbyte[] Heightmap = [];
    public SubChunkRenderHeightmapType RenderHeightmapType;
    public sbyte[] RenderHeightmap = [];

    public override void Read(ref BinaryReader reader) {
        HeightmapType = (SubChunkHeightmapType)reader.ReadUInt8();
        if (!Enum.IsDefined(HeightmapType))
            throw new InvalidDataException("Subchunk heightmap type is invalid.");
        if (reader.ReadBool()) {
            Heightmap = new sbyte[256];
            for (int z = 0; z < 16; z++) {
                uint rowLength = reader.ReadVarUInt();
                if (rowLength != 16)
                    throw new InvalidDataException($"Subchunk heightmap row length {rowLength} is invalid.");
                for (int x = 0; x < 16; x++)
                    Heightmap[z * 16 + x] = reader.ReadInt8();
            }
        }
        else {
            Heightmap = [];
        }

        RenderHeightmapType = (SubChunkRenderHeightmapType)reader.ReadUInt8();
        if (!Enum.IsDefined(RenderHeightmapType))
            throw new InvalidDataException("Subchunk render heightmap type is invalid.");
        if (reader.ReadBool()) {
            RenderHeightmap = new sbyte[256];
            for (int z = 0; z < 16; z++) {
                uint rowLength = reader.ReadVarUInt();
                if (rowLength != 16)
                    throw new InvalidDataException($"Subchunk render heightmap row length {rowLength} is invalid.");
                for (int x = 0; x < 16; x++)
                    RenderHeightmap[z * 16 + x] = reader.ReadInt8();
            }
        }
        else {
            RenderHeightmap = [];
        }
    }

    public override void Write(ref BinaryWriter writer) {
        if (!Enum.IsDefined(HeightmapType) || !Enum.IsDefined(RenderHeightmapType))
            throw new InvalidDataException("Subchunk heightmap type is invalid.");
        if (HeightmapType == SubChunkHeightmapType.HasData && Heightmap.Length != 256)
            throw new InvalidDataException("Subchunk heightmap must contain 256 values.");
        if (RenderHeightmapType == SubChunkRenderHeightmapType.HasData && RenderHeightmap.Length != 256)
            throw new InvalidDataException("Subchunk render heightmap must contain 256 values.");

        writer.WriteUInt8((byte)HeightmapType);
        writer.WriteBool(HeightmapType == SubChunkHeightmapType.HasData);
        if (HeightmapType == SubChunkHeightmapType.HasData) {
            for (int z = 0; z < 16; z++) {
                writer.WriteVarUInt(16);
                for (int x = 0; x < 16; x++)
                    writer.WriteInt8(Heightmap[z * 16 + x]);
            }
        }
        writer.WriteUInt8((byte)RenderHeightmapType);
        writer.WriteBool(RenderHeightmapType == SubChunkRenderHeightmapType.HasData);
        if (RenderHeightmapType == SubChunkRenderHeightmapType.HasData) {
            for (int z = 0; z < 16; z++) {
                writer.WriteVarUInt(16);
                for (int x = 0; x < 16; x++)
                    writer.WriteInt8(RenderHeightmap[z * 16 + x]);
            }
        }
    }
}
