using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public class OptionalValue<T>
{
    public delegate T ReaderDelegate(BinaryReader reader);
    public delegate T ReaderDelegate<TParameter>(BinaryReader reader, TParameter parameter);
    public delegate void WriterDelegate(BinaryWriter writer, T value);
    public delegate void WriterDelegate<TParameter>(BinaryWriter writer, T value, TParameter parameter);

    public bool HasValue { get; set; }
    public T? Value { get; set; }

    public void Read(BinaryReader reader, ReaderDelegate read)
    {
        HasValue = reader.ReadBool();
        if (!HasValue)
        {
            Value = default;
            return;
        }

        Value = read(reader);
    }

    public void Write(BinaryWriter writer, WriterDelegate write)
    {
        writer.WriteBool(HasValue);
        if (!HasValue)
        {
            return;
        }

        if (Value is null)
        {
            throw new InvalidOperationException("Optional value is marked as present but Value is null.");
        }

        write(writer, Value);
    }

    public void Read<TParameter>(BinaryReader reader, TParameter parameter, ReaderDelegate<TParameter> read)
    {
        HasValue = reader.ReadBool();
        if (!HasValue)
        {
            Value = default;
            return;
        }

        Value = read(reader, parameter);
    }

    public void Write<TParameter>(BinaryWriter writer, TParameter parameter, WriterDelegate<TParameter> write)
    {
        writer.WriteBool(HasValue);
        if (!HasValue)
        {
            return;
        }

        if (Value is null)
        {
            throw new InvalidOperationException("Optional value is marked as present but Value is null.");
        }

        write(writer, Value, parameter);
    }
}

