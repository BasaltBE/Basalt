using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Types;

public sealed class DataStoreUpdate : DataStoreChangeInfo
{
    public override DataStoreChangeAction Action => DataStoreChangeAction.Update;
    public override string DataStoreName { get; set; } = string.Empty;
    public string Property = string.Empty;
    public string Path = string.Empty;
    public object Value = 0d;
    public uint PropertyUpdateCount;
    public uint PathUpdateCount;

    public override void Read(Binary.BinaryReader reader)
    {
        DataStoreName = reader.ReadVarString();
        Property = reader.ReadVarString();
        Path = reader.ReadVarString();
        int valueType = reader.ReadVarInt();
        Value = valueType switch
        {
            0 => reader.ReadF64(true),
            1 => reader.ReadBool(),
            2 => reader.ReadVarString(),
            _ => throw new NotSupportedException($"Unsupported data store update value type {valueType}.")
        };
        PropertyUpdateCount = reader.ReadUInt32(true);
        PathUpdateCount = reader.ReadUInt32(true);
    }

    public override void Write(Binary.BinaryWriter writer)
    {
        writer.WriteVarString(DataStoreName);
        writer.WriteVarString(Property);
        writer.WriteVarString(Path);
        switch (Value)
        {
            case float value:
                writer.WriteVarInt(0);
                writer.WriteF64(value, true);
                break;
            case double value:
                writer.WriteVarInt(0);
                writer.WriteF64(value, true);
                break;
            case int value:
                writer.WriteVarInt(0);
                writer.WriteF64(value, true);
                break;
            case bool value:
                writer.WriteVarInt(1);
                writer.WriteBool(value);
                break;
            case string value:
                writer.WriteVarInt(2);
                writer.WriteVarString(value);
                break;
            default:
                throw new NotSupportedException($"Unsupported data store update value type {Value.GetType()}.");
        }

        writer.WriteUInt32(PropertyUpdateCount, true);
        writer.WriteUInt32(PathUpdateCount, true);
    }
}
