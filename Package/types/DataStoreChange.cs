#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class DataStoreChange : ClientboundDataStorePayloadUpdateVariant {
    public string DataStoreName = string.Empty;
    public string Property = string.Empty;
    public uint UpdateCount;
    public object TheNewPropertyValue = null!;

    public delegate object TheNewPropertyValueReader(BinaryReader reader);
    public delegate void TheNewPropertyValueWriter(BinaryWriter writer, object value);

    public void Read(BinaryReader reader) {
        throw new NotSupportedException("DataStoreChange requires external reader callbacks for: TheNewPropertyValue. Use the Read overload that accepts them.");
    }

    public void Read(BinaryReader reader, TheNewPropertyValueReader readTheNewPropertyValue) {
        DataStoreName = reader.ReadVarString();
        Property = reader.ReadVarString();
        UpdateCount = reader.ReadUInt32(true);
        TheNewPropertyValue = readTheNewPropertyValue(reader);
    }

    public void Write(BinaryWriter writer) {
        throw new NotSupportedException("DataStoreChange requires external writer callbacks for: TheNewPropertyValue. Use the Write overload that accepts them.");
    }

    public void Write(BinaryWriter writer, TheNewPropertyValueWriter writeTheNewPropertyValue) {
        writer.WriteVarString(DataStoreName);
        writer.WriteVarString(Property);
        writer.WriteUInt32(UpdateCount, true);
        writeTheNewPropertyValue(writer, TheNewPropertyValue);
    }
}
