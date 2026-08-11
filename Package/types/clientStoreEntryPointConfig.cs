using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class clientStoreEntryPointConfig {
    public string StoreId = string.Empty;
    public string StoreName = string.Empty;

    public void Read(BinaryReader reader) {
        StoreId = reader.ReadVarString();
        StoreName = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(StoreId);
        writer.WriteVarString(StoreName);
    }
}
