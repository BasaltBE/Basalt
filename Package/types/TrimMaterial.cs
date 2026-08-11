using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class TrimMaterial {
    public string MaterialId = string.Empty;
    public string Color = string.Empty;
    public string ItemName = string.Empty;

    public void Read(BinaryReader reader) {
        MaterialId = reader.ReadVarString();
        Color = reader.ReadVarString();
        ItemName = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(MaterialId);
        writer.WriteVarString(Color);
        writer.WriteVarString(ItemName);
    }
}
