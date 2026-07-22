namespace Basalt.Protocol.Types;

using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

public sealed class Optional<T> : OptionalValue<T> where T : DataType, new() {
    public void Read(BinaryReader reader) {
        HasValue = reader.ReadBool();
        if (!HasValue) {
            Value = default;
            return;
        }

        T value = new();
        value.Read(reader);
        Value = value;
    }

    public void Write(BinaryWriter writer) {
        writer.WriteBool(HasValue);
        if (!HasValue) {
            return;
        }

        if (Value is null) {
            throw new InvalidOperationException("Optional value is marked as present but Value is null.");
        }

        Value.Write(writer);
    }

    public void Read<TParameter>(BinaryReader reader, TParameter parameter) {
        HasValue = reader.ReadBool();
        if (!HasValue) {
            Value = default;
            return;
        }

        T value = new();
        if (value is not DataType<TParameter> parameterized) {
            throw new InvalidOperationException($"{typeof(T).Name} does not implement DataType<{typeof(TParameter).Name}>.");
        }

        parameterized.Read(reader, parameter);
        Value = value;
    }

    public void Write<TParameter>(BinaryWriter writer, TParameter parameter) {
        writer.WriteBool(HasValue);
        if (!HasValue) {
            return;
        }

        if (Value is null) {
            throw new InvalidOperationException("Optional value is marked as present but Value is null.");
        }

        if (Value is not DataType<TParameter> parameterized) {
            throw new InvalidOperationException($"{typeof(T).Name} does not implement DataType<{typeof(TParameter).Name}>.");
        }

        parameterized.Write(writer, parameter);
    }
}

