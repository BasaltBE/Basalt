using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class OptionalValue<T>
{
    public delegate T ReaderDelegate(ref BinaryReader reader);
    public delegate void WriterDelegate(ref BinaryWriter writer, T value);

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
}

