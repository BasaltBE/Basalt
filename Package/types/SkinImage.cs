#nullable enable

using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SkinImage {
    public uint Width;
    public uint Height;
    public List<byte> ImageBytes = [];

    public void Read(BinaryReader reader) {
        Width = reader.ReadUInt32(true);
        Height = reader.ReadUInt32(true);
        int count4 = checked((int)reader.ReadVarUInt());
        ImageBytes = new List<byte>(count4);
        for (int i4 = 0; i4 < count4; i4++) {
            byte item4 = default!;
            item4 = reader.ReadUInt8();
            ImageBytes.Add(item4);
        }
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt32(Width, true);
        writer.WriteUInt32(Height, true);
        writer.WriteVarUInt(checked((uint)ImageBytes.Count));
        foreach (var item5 in ImageBytes) {
            writer.WriteUInt8(item5);
        }
    }
}
