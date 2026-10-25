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
        if (reader.ReadBool()) {
            Heightmap = new sbyte[256];
            for (int i = 0; i < Heightmap.Length; i++)
                Heightmap[i] = reader.ReadInt8();
        }
        else {
            Heightmap = [];
        }

        RenderHeightmapType = (SubChunkRenderHeightmapType)reader.ReadUInt8();
        if (reader.ReadBool()) {
            RenderHeightmap = new sbyte[256];
            for (int i = 0; i < RenderHeightmap.Length; i++)
                RenderHeightmap[i] = reader.ReadInt8();
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
            foreach (sbyte height in Heightmap)
                writer.WriteInt8(height);
        }
        writer.WriteUInt8((byte)RenderHeightmapType);
        writer.WriteBool(RenderHeightmapType == SubChunkRenderHeightmapType.HasData);
        if (RenderHeightmapType == SubChunkRenderHeightmapType.HasData) {
            foreach (sbyte height in RenderHeightmap)
                writer.WriteInt8(height);
        }
    }
}
