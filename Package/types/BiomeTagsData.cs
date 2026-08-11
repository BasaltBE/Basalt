using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class BiomeTagsData {
    public List<ushort> Tags = [];

    public void Read(BinaryReader reader) {
        int count0 = checked((int)reader.ReadVarUInt());
        Tags = new List<ushort>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            ushort item0 = default!;
            item0 = reader.ReadUInt16(true);
            Tags.Add(item0);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(checked((uint)Tags.Count));
        foreach (var item1 in Tags) {
            writer.WriteUInt16(item1, true);
        }
    }
}
