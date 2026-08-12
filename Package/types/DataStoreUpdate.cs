#nullable enable

using System;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BedrockProtocol.Types;

public sealed class DataStoreUpdate : ClientboundDataStorePayloadUpdateVariant {
    public string DataStoreName = string.Empty;
    public string Property = string.Empty;
    public string Path = string.Empty;
    public DataStoreUpdateDataValue Data = null!;
    public uint PropertyUpdateCount;
    public uint PathUpdateCount;

    public void Read(BinaryReader reader) {
        DataStoreName = reader.ReadVarString();
        Property = reader.ReadVarString();
        Path = reader.ReadVarString();
        Data.Read(reader);
        PropertyUpdateCount = reader.ReadUInt32(true);
        PathUpdateCount = reader.ReadUInt32(true);
    }

    public void Write(BinaryWriter writer) {
        writer.WriteVarString(DataStoreName);
        writer.WriteVarString(Property);
        writer.WriteVarString(Path);
        Data.Write(writer);
        writer.WriteUInt32(PropertyUpdateCount, true);
        writer.WriteUInt32(PathUpdateCount, true);
    }
}
