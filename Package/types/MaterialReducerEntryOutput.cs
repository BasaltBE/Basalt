#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class MaterialReducerEntryOutput {
    public int ItemId;
    public int ItemCount;

    public void Read(BinaryReader reader) {
        ItemId = reader.ReadZigZag();
        ItemCount = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(ItemId);
        writer.WriteZigZag(ItemCount);
    }
}
