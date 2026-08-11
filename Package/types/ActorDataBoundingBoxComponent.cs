using System;
using System.Collections.Generic;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ActorDataBoundingBoxComponent {
    public List<float> ActorDataBoundingBox = [];

    public void Read(BinaryReader reader) {
        int count0 = 3;
        ActorDataBoundingBox = new List<float>(count0);
        for (int i0 = 0; i0 < count0; i0++) {
            float item0 = default!;
            item0 = reader.ReadF32(true);
            ActorDataBoundingBox.Add(item0);
        }
    }

    public void Write(BinaryWriter writer) {
        foreach (var item1 in ActorDataBoundingBox) {
            writer.WriteF32(item1, true);
        }
    }
}
