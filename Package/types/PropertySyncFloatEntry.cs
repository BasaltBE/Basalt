#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class PropertySyncFloatEntry {
    public uint PropertyIndex;
    public float Data;

    public void Read(BinaryReader reader) {
        PropertyIndex = reader.ReadVarUInt();
        Data = reader.ReadF32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarUInt(PropertyIndex);
        writer.WriteF32(Data, true);
    }
}
