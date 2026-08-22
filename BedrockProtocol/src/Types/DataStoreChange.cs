using Basalt.BedrockProtocol.Enums;
using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class DataStoreChange : DataType {
    public string DataStoreName = string.Empty;
    public string Property = string.Empty;
    public uint UpdateCount;
    public DataStoreValueType ValueType;
    public double DoubleValue;
    public bool BoolValue;
    public string StringValue = string.Empty;

    public override void Write(ref BinaryWriter writer) {
        writer.WriteVarString(DataStoreName);
        writer.WriteVarString(Property);
        writer.WriteUInt32(UpdateCount, true);
        writer.WriteVarUInt((uint)ValueType);
        switch (ValueType) {
            case DataStoreValueType.Double: writer.WriteF64(DoubleValue, true); break;
            case DataStoreValueType.Bool: writer.WriteBool(BoolValue); break;
            case DataStoreValueType.String: writer.WriteVarString(StringValue); break;
            default: throw new ArgumentOutOfRangeException(nameof(ValueType));
        }
    }

    public override void Read(ref BinaryReader reader) {
        DataStoreName = reader.ReadVarString();
        Property = reader.ReadVarString();
        UpdateCount = reader.ReadUInt32(true);
        ValueType = (DataStoreValueType)reader.ReadVarUInt();
        switch (ValueType) {
            case DataStoreValueType.Double: DoubleValue = reader.ReadF64(true); break;
            case DataStoreValueType.Bool: BoolValue = reader.ReadBool(); break;
            case DataStoreValueType.String: StringValue = reader.ReadVarString(); break;
            default: throw new FormatException("Unsupported data store value type.");
        }
    }
}
