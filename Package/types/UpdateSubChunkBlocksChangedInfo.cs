using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class UpdateSubChunkBlocksChangedInfo {
    public List<UpdateSubChunkNetworkBlockInfo> BlocksChangedStandards = [];
    public List<UpdateSubChunkNetworkBlockInfo> BlocksChangedExtras = [];

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        BlocksChangedStandards = new List<UpdateSubChunkNetworkBlockInfo>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            UpdateSubChunkNetworkBlockInfo item0 = default!;
            UpdateSubChunkNetworkBlockInfo readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            BlocksChangedStandards.Add(item0);
        }
        int count2 = checked((int)reader.ReadVarUInt());
        BlocksChangedExtras = new List<UpdateSubChunkNetworkBlockInfo>(count2);
        for (int i2 = 0; i2 < count2; i2++) {
            UpdateSubChunkNetworkBlockInfo item2 = default!;
            UpdateSubChunkNetworkBlockInfo readValue1002 = new();
            readValue1002.Read(reader);
            item2 = readValue1002;
            BlocksChangedExtras.Add(item2);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)BlocksChangedStandards.Count));
        foreach (var item1 in BlocksChangedStandards) {
            item1.Write(writer);
        }
        writer.WriteVarUInt(checked((uint)BlocksChangedExtras.Count));
        foreach (var item3 in BlocksChangedExtras) {
            item3.Write(writer);
        }
    }
}
