#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ContainerMixDataEntry {
    public int FromItemId;
    public int ReagentItemId;
    public int ToItemId;

    public void Read(BinaryReader reader) {
        FromItemId = reader.ReadZigZag();
        ReagentItemId = reader.ReadZigZag();
        ToItemId = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(FromItemId);
        writer.WriteZigZag(ReagentItemId);
        writer.WriteZigZag(ToItemId);
    }
}
