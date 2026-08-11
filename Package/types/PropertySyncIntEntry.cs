using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PropertySyncIntEntry {
    public uint PropertyIndex;
    public int Data;

    public void Read(BinaryReader reader) {
        PropertyIndex = reader.ReadVarUInt();
        Data = reader.ReadZigZag();
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(PropertyIndex);
        writer.WriteZigZag(Data);
    }
}
