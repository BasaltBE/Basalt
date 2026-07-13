using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Types;

public sealed class DataStorePropertyValue
{
    public DataStorePropertyValueType Type;
    public object? Value;

    public static DataStorePropertyValue None() => new() { Type = DataStorePropertyValueType.None };
    public static DataStorePropertyValue Boolean(bool value) => new() { Type = DataStorePropertyValueType.Boolean, Value = value };
    public static DataStorePropertyValue Int64(long value) => new() { Type = DataStorePropertyValueType.Int64, Value = value };
    public static DataStorePropertyValue String(string value) => new() { Type = DataStorePropertyValueType.String, Value = value };
    public static DataStorePropertyValue TypeValue(Dictionary<string, DataStorePropertyValue> value) => new() { Type = DataStorePropertyValueType.Type, Value = value };

    public void Read(Binary.BinaryReader reader)
    {
        Type = (DataStorePropertyValueType)reader.ReadUInt32(true);
        Value = Type switch
        {
            DataStorePropertyValueType.None => null,
            DataStorePropertyValueType.Boolean => reader.ReadBool(),
            DataStorePropertyValueType.Int64 => reader.ReadInt64(true),
            DataStorePropertyValueType.String => reader.ReadVarString(),
            DataStorePropertyValueType.Type => ReadObject(reader),
            _ => throw new NotSupportedException($"Unsupported data store value type {Type}.")
        };
    }

    public void Write(Binary.BinaryWriter writer)
    {
        writer.WriteUInt32((uint)Type, true);
        switch (Type)
        {
            case DataStorePropertyValueType.None:
                break;
            case DataStorePropertyValueType.Boolean:
                writer.WriteBool((bool)(Value ?? false));
                break;
            case DataStorePropertyValueType.Int64:
                writer.WriteInt64(Convert.ToInt64(Value), true);
                break;
            case DataStorePropertyValueType.String:
                writer.WriteVarString(Convert.ToString(Value) ?? string.Empty);
                break;
            case DataStorePropertyValueType.Type:
                WriteObject(writer, Value as Dictionary<string, DataStorePropertyValue> ?? []);
                break;
            default:
                throw new NotSupportedException($"Unsupported data store value type {Type}.");
        }
    }

    static Dictionary<string, DataStorePropertyValue> ReadObject(Binary.BinaryReader reader)
    {
        int count = reader.ReadVarInt();
        Dictionary<string, DataStorePropertyValue> value = new(count);
        for (int i = 0; i < count; i++)
        {
            string key = reader.ReadVarString();
            DataStorePropertyValue property = new();
            property.Read(reader);
            value[key] = property;
        }

        return value;
    }

    static void WriteObject(Binary.BinaryWriter writer, Dictionary<string, DataStorePropertyValue> value)
    {
        writer.WriteVarInt(value.Count);
        foreach ((string key, DataStorePropertyValue property) in value)
        {
            writer.WriteVarString(key);
            property.Write(writer);
        }
    }
}
