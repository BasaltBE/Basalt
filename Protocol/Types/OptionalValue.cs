using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public class OptionalValue<T>
{
    public delegate T ReaderDelegate(ref BinaryReader reader);
    public delegate T ReaderDelegate<TParameter>(ref BinaryReader reader, TParameter parameter);
    public delegate void WriterDelegate(ref BinaryWriter writer, T value);
    public delegate void WriterDelegate<TParameter>(ref BinaryWriter writer, T value, TParameter parameter);

    public bool HasValue { get; set; }
    public T? Value { get; set; }

    public void Read(ref BinaryReader reader, ReaderDelegate read)
    {
        HasValue = reader.ReadBool();
        if (!HasValue)
        {
            Value = default;
            return;
        }

        Value = read(ref reader);
    }

    public void Write(ref BinaryWriter writer, WriterDelegate write)
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

        write(ref writer, Value);
    }

    public void Read<TParameter>(ref BinaryReader reader, TParameter parameter, ReaderDelegate<TParameter> read)
    {
        HasValue = reader.ReadBool();
        if (!HasValue)
        {
            Value = default;
            return;
        }

        Value = read(ref reader, parameter);
    }

    public void Write<TParameter>(ref BinaryWriter writer, TParameter parameter, WriterDelegate<TParameter> write)
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

        write(ref writer, Value, parameter);
    }
}

