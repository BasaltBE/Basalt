using Basalt.Binary;
using Basalt.BedrockProtocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class DataStoreUpdate : DataType {
    public string DataStoreName = string.Empty;
    public string Property = string.Empty;
    public string Path = string.Empty;
    public DataStoreValueType ValueType;
    public double DoubleValue;
    public bool BoolValue;
    public string StringValue = string.Empty;
    public uint PropertyUpdateCount;
    public uint PathUpdateCount;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(DataStoreName);
        writer.WriteVarString(Property);
        writer.WriteVarString(Path);
        writer.WriteVarUInt((uint)ValueType);

        switch (ValueType) {
            case DataStoreValueType.Double:
                writer.WriteF64(DoubleValue, true);
                break;
            case DataStoreValueType.Bool:
                writer.WriteBool(BoolValue);
                break;
            case DataStoreValueType.String:
                writer.WriteVarString(StringValue);
                break;
        }

        writer.WriteUInt32(PropertyUpdateCount, true);
        writer.WriteUInt32(PathUpdateCount, true);
    }

    public override void Read(ref BinaryReader reader) {
        DataStoreName = reader.ReadVarString();
        Property = reader.ReadVarString();
        Path = reader.ReadVarString();
        ValueType = (DataStoreValueType)reader.ReadVarUInt();

        switch (ValueType) {
            case DataStoreValueType.Double:
                DoubleValue = reader.ReadF64(true);
                break;
            case DataStoreValueType.Bool:
                BoolValue = reader.ReadBool();
                break;
            case DataStoreValueType.String:
                StringValue = reader.ReadVarString();
                break;
        }

        PropertyUpdateCount = reader.ReadUInt32(true);
        PathUpdateCount = reader.ReadUInt32(true);
    }
}
