namespace Basalt.Protocol.Types;

using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

public sealed class Optional<T> : OptionalValue<T> where T : DataType, new()
{
    public void Read(ref BinaryReader reader)
    {
        HasValue = reader.ReadBool();
        if (!HasValue)
        {
            Value = default;
            return;
        }

        T value = new();
        value.Read(ref reader);
        Value = value;
    }

    public void Write(ref BinaryWriter writer)
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

        Value.Write(ref writer);
    }
}

