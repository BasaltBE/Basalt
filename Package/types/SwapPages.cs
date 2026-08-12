#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class SwapPages : BookEditOperationVariant {
    public int PageIndex;
    public int SwapWithIndex;

    public void Read(BinaryReader reader) {
        PageIndex = reader.ReadZigZag();
        SwapWithIndex = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(PageIndex);
        writer.WriteZigZag(SwapWithIndex);
    }
}
