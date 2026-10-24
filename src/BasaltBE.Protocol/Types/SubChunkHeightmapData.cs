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
        if (HeightmapType == SubChunkHeightmapType.HasData) {
            if (reader.ReadVarUInt() != 16)
                throw new InvalidDataException("Subchunk heightmap must contain 16 rows.");

            Heightmap = new sbyte[256];
            int offset = 0;
            for (int z = 0; z < 16; z++) {
                if (reader.ReadVarUInt() != 16)
                    throw new InvalidDataException("Subchunk heightmap rows must contain 16 values.");
                for (int x = 0; x < 16; x++)
                    Heightmap[offset++] = reader.ReadInt8();
            }
        }
        else {
            Heightmap = [];
        }

        RenderHeightmapType = (SubChunkRenderHeightmapType)reader.ReadUInt8();
        if (RenderHeightmapType == SubChunkRenderHeightmapType.HasData) {
            if (reader.ReadVarUInt() != 16)
                throw new InvalidDataException("Subchunk heightmap must contain 16 rows.");

            RenderHeightmap = new sbyte[256];
            int offset = 0;
            for (int z = 0; z < 16; z++) {
                if (reader.ReadVarUInt() != 16)
                    throw new InvalidDataException("Subchunk heightmap rows must contain 16 values.");
                for (int x = 0; x < 16; x++)
                    RenderHeightmap[offset++] = reader.ReadInt8();
            }
        }
        else {
            RenderHeightmap = [];
        }

        if (!Enum.IsDefined(HeightmapType) || !Enum.IsDefined(RenderHeightmapType))
            throw new InvalidDataException("Subchunk heightmap type is invalid.");
    }

    public override void Write(ref BinaryWriter writer) {
        if (!Enum.IsDefined(HeightmapType) || !Enum.IsDefined(RenderHeightmapType))
            throw new InvalidDataException("Subchunk heightmap type is invalid.");
        if (HeightmapType == SubChunkHeightmapType.HasData && Heightmap.Length != 256)
            throw new InvalidDataException("Subchunk heightmap must contain 256 values.");
        if (RenderHeightmapType == SubChunkRenderHeightmapType.HasData && RenderHeightmap.Length != 256)
            throw new InvalidDataException("Subchunk render heightmap must contain 256 values.");

        writer.WriteUInt8((byte)HeightmapType);
        if (HeightmapType == SubChunkHeightmapType.HasData) {
            writer.WriteVarUInt(16);
            for (int z = 0; z < 16; z++) {
                writer.WriteVarUInt(16);
                for (int x = 0; x < 16; x++)
                    writer.WriteInt8(Heightmap[z * 16 + x]);
            }
        }
        writer.WriteUInt8((byte)RenderHeightmapType);
        if (RenderHeightmapType == SubChunkRenderHeightmapType.HasData) {
            writer.WriteVarUInt(16);
            for (int z = 0; z < 16; z++) {
                writer.WriteVarUInt(16);
                for (int x = 0; x < 16; x++)
                    writer.WriteInt8(RenderHeightmap[z * 16 + x]);
            }
        }
    }
}
