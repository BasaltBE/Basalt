using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class Attribute : DataType
{
    public float Min { get; set; }
    public float Max { get; set; }
    public float Current { get; set; }
    public float Default { get; set; }
    public AttributeName Name { get; set; }

    public Attribute(float min, float max, float current, float defaultValue, AttributeName name)
    {
        Min = min;
        Max = max;
        Current = current;
        Default = defaultValue;
        Name = name;
    }

    public Attribute()
    {
    }

    public void Read(ref BinaryReader reader)
    {
        Min = reader.ReadF32(true);
        Max = reader.ReadF32(true);
        Current = reader.ReadF32(true);
        Default = reader.ReadF32(true);
        Name = AttributeNameHelper.FromProtocolString(reader.ReadVarString());
    }

    public void Write(ref BinaryWriter writer)
    {
        writer.WriteF32(Min, true);
        writer.WriteF32(Max, true);
        writer.WriteF32(Current, true);
        writer.WriteF32(Default, true);
        writer.WriteVarString(Name.ToProtocolString());
    }

    public static List<Attribute> ReadList(ref BinaryReader reader)
    {
        int count = checked((int)reader.ReadVarUInt());
        List<Attribute> attributes = new(count);
        for (int i = 0; i < count; i++)
        {
            Attribute attribute = new();
            attribute.Read(ref reader);
            attributes.Add(attribute);
        }

        return attributes;
    }

    public static void WriteList(ref BinaryWriter writer, IReadOnlyList<Attribute> attributes)
    {
        writer.WriteVarUInt((uint)attributes.Count);
        for (int i = 0; i < attributes.Count; i++)
        {
            attributes[i].Write(ref writer);
        }
    }
}
