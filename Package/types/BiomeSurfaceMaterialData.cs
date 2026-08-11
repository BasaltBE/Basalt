using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeSurfaceMaterialData {
    public uint TopBlock;
    public uint MidBlock;
    public uint SeaFloorBlock;
    public uint FoundationBlock;
    public uint SeaBlock;
    public int SeaFloorDepth;

    public void Read(BinaryReader reader) {
        TopBlock = reader.ReadUInt32(true);
        MidBlock = reader.ReadUInt32(true);
        SeaFloorBlock = reader.ReadUInt32(true);
        FoundationBlock = reader.ReadUInt32(true);
        SeaBlock = reader.ReadUInt32(true);
        SeaFloorDepth = reader.ReadInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt32(TopBlock, true);
        writer.WriteUInt32(MidBlock, true);
        writer.WriteUInt32(SeaFloorBlock, true);
        writer.WriteUInt32(FoundationBlock, true);
        writer.WriteUInt32(SeaBlock, true);
        writer.WriteInt32(SeaFloorDepth, true);
    }
}
