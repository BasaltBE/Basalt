using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class DataStoreRemoval : ClientboundDataStorePayloadUpdateVariant {
    public string DataStoreName = string.Empty;

    public void Read(BinaryReader reader) {
        DataStoreName = reader.ReadVarString();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(DataStoreName);
    }
}
