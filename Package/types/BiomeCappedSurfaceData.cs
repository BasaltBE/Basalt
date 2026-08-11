using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeCappedSurfaceData {
    public List<uint> FloorBlocks = [];
    public List<uint> CeilingBlocks = [];
    public uint SeaBlock;
    public uint FoundationBlock;
    public uint BeachBlock;

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        FloorBlocks = new List<uint>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            uint item0 = default!;
            item0 = reader.ReadUInt32(true);
            FloorBlocks.Add(item0);
        }
        int count2 = checked((int)reader.ReadVarUInt());
        CeilingBlocks = new List<uint>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            uint item2 = default!;
            item2 = reader.ReadUInt32(true);
            CeilingBlocks.Add(item2);
        }
        SeaBlock = reader.ReadUInt32(true);
        FoundationBlock = reader.ReadUInt32(true);
        BeachBlock = reader.ReadUInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)FloorBlocks.Count));
        foreach (var item1 in FloorBlocks) {
            writer.WriteUInt32(item1, true);
        }
        writer.WriteVarUInt(checked((uint)CeilingBlocks.Count));
        foreach (var item3 in CeilingBlocks) {
            writer.WriteUInt32(item3, true);
        }
        writer.WriteUInt32(SeaBlock, true);
        writer.WriteUInt32(FoundationBlock, true);
        writer.WriteUInt32(BeachBlock, true);
    }
}
