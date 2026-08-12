#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class DeletePage : BookEditOperationVariant {
    public int PageIndex;

    public void Read(BinaryReader reader) {
        PageIndex = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(PageIndex);
    }
}
