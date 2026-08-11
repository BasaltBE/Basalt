using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class ReplacePage : BookEditOperationVariant {
    public int PageIndex;
    public string PageText = string.Empty;
    public string PhotoName = string.Empty;

    public void Read(BinaryReader reader) {
        PageIndex = reader.ReadZigZag();
        PageText = reader.ReadVarString();
        PhotoName = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteZigZag(PageIndex);
        writer.WriteVarString(PageText);
        writer.WriteVarString(PhotoName);
    }
}
