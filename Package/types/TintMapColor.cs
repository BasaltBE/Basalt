using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class TintMapColor {
    public List<Color> Colors = [];

    public void Read(BinaryReader reader) {
        int count0 = 4;
        Colors = new List<Color>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            Color item0 = default!;
            Color readValue1000 = new();
            readValue1000.Read(reader);
            item0 = readValue1000;
            Colors.Add(item0);
        }
    }

    public void Write(BinaryWriter writer) {
        foreach (var item1 in Colors) {
            item1.Write(writer);
        }
    }
}
