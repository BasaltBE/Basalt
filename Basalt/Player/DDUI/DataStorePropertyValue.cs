using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

public sealed class DataStorePropertyValue {
    public DataStorePropertyValueType Type;
    public object? Value;

    public static DataStorePropertyValue None() =>
        new() {
            Type = DataStorePropertyValueType.None
        };

    public static DataStorePropertyValue Null() =>
        new() {
            Type = DataStorePropertyValueType.Null
        };

    public static DataStorePropertyValue Boolean(bool value) =>
        new() {
            Type = DataStorePropertyValueType.Boolean,
            Value = value
        };

    public static DataStorePropertyValue Int64(long value) =>
        new() {
            Type = DataStorePropertyValueType.Int64,
            Value = value
        };

    public static DataStorePropertyValue Double(double value) =>
        new() {
            Type = DataStorePropertyValueType.Double,
            Value = value
        };

    public static DataStorePropertyValue String(string value) =>
        new() {
            Type = DataStorePropertyValueType.String,
            Value = value
        };

    public static DataStorePropertyValue TypeValue(
        Dictionary<string, DataStorePropertyValue> value
    ) =>
        new() {
            Type = DataStorePropertyValueType.Type,
            Value = value
        };

    public void Read(BinaryReader reader) {
        Type = (DataStorePropertyValueType)reader.ReadUInt32(true);

        Value = Type switch {
            DataStorePropertyValueType.None => null,
            DataStorePropertyValueType.Boolean => reader.ReadBool(),
            DataStorePropertyValueType.Int64 => reader.ReadInt64(true),
            DataStorePropertyValueType.Double => reader.ReadF64(true),
            DataStorePropertyValueType.String => reader.ReadVarString(),
            DataStorePropertyValueType.Null => null,
            DataStorePropertyValueType.Type => ReadProperties(reader),

            _ => throw new NotSupportedException(
                $"Unsupported data store property value type: {Type}."
            )
        };
    }

    public void Write(BinaryWriter writer) {
        writer.WriteUInt32((uint)Type, true);

        switch (Type) {
            case DataStorePropertyValueType.None:
            case DataStorePropertyValueType.Null:
                return;

            case DataStorePropertyValueType.Boolean:
                writer.WriteBool(GetValue<bool>());
                return;

            case DataStorePropertyValueType.Int64:
                writer.WriteInt64(GetValue<long>(), true);
                return;

            case DataStorePropertyValueType.Double:
                writer.WriteF64(GetValue<double>(), true);
                return;

            case DataStorePropertyValueType.String:
                writer.WriteVarString(GetValue<string>());
                return;

            case DataStorePropertyValueType.Type:
                WriteProperties(
                    writer,
                    GetValue<Dictionary<string, DataStorePropertyValue>>()
                );
                return;

            default:
                throw new NotSupportedException(
                    $"Unsupported data store property value type: {Type}."
                );
        }
    }

    public static object ReadDynamicValue(BinaryReader reader) {
        DataStorePropertyValue value = new();
        value.Read(reader);
        return value;
    }

    public static void WriteDynamicValue(BinaryWriter writer, object value) {
        if (value is not DataStorePropertyValue propertyValue) {
            throw new InvalidOperationException(
                $"Expected {nameof(DataStorePropertyValue)}, but received " +
                $"{value?.GetType().FullName ?? "null"}."
            );
        }

        propertyValue.Write(writer);
    }

    public T GetValue<T>() {
        if (Value is T value) {
            return value;
        }

        throw new InvalidOperationException(
            $"Data store value type {Type} does not contain a " +
            $"{typeof(T).FullName} value."
        );
    }

    private static Dictionary<string, DataStorePropertyValue> ReadProperties(
        BinaryReader reader
    ) {
        int count = reader.ReadVarInt();

        if (count < 0) {
            throw new InvalidDataException(
                $"Invalid data store property count: {count}."
            );
        }

        Dictionary<string, DataStorePropertyValue> properties = new(count);

        for (int i = 0; i < count; i++) {
            string key = reader.ReadVarString();

            DataStorePropertyValue property = new();
            property.Read(reader);

            properties[key] = property;
        }

        return properties;
    }

    private static void WriteProperties(
        BinaryWriter writer,
        Dictionary<string, DataStorePropertyValue> properties
    ) {
        writer.WriteVarInt(properties.Count);

        foreach (
            KeyValuePair<string, DataStorePropertyValue> entry in properties
        ) {
            writer.WriteVarString(entry.Key);
            entry.Value.Write(writer);
        }
    }
}