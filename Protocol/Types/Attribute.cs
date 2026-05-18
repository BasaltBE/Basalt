using Basalt.Protocol.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public sealed class Attribute : DataType
{
    public float Min { get; set; }
    public float Max { get; set; }
    public float Current { get; set; }
    public float DefaultMin { get; set; }
    public float DefaultMax { get; set; }
    public float Default { get; set; }
    public AttributeName Name { get; set; }

    public Attribute(float min, float max, float current, float defaultValue, AttributeName name)
    {
        Min = min;
        Max = max;
        Current = current;
        DefaultMin = min;
        DefaultMax = max;
        Default = defaultValue;
        Name = name;
    }

    public Attribute()
    {
    }

    public void Read(BinaryReader reader)
    {
        Min = reader.ReadF32(true);
        Max = reader.ReadF32(true);
        Current = reader.ReadF32(true);
        DefaultMin = reader.ReadF32(true);
        DefaultMax = reader.ReadF32(true);
        Default = reader.ReadF32(true);
        Name = AttributeNameHelper.FromProtocolString(reader.ReadVarString());
        int modifiers = reader.ReadVarInt();
        for (int i = 0; i < modifiers; i++)
        {
            _ = reader.ReadVarString();
            _ = reader.ReadVarString();
            _ = reader.ReadVarString();
            _ = reader.ReadF32(true);
            _ = reader.ReadInt32(true);
            _ = reader.ReadBool();
        }
    }

    public void Write(BinaryWriter writer)
    {
        writer.WriteF32(Min, true);
        writer.WriteF32(Max, true);
        writer.WriteF32(Current, true);
        writer.WriteF32(DefaultMin, true);
        writer.WriteF32(DefaultMax, true);
        writer.WriteF32(Default, true);
        writer.WriteVarString(Name.ToProtocolString());
        writer.WriteVarInt(0);
    }

    public static List<Attribute> ReadList(BinaryReader reader)
    {
        int count = reader.ReadVarInt();
        List<Attribute> attributes = new(count);
        for (int i = 0; i < count; i++)
        {
            Attribute attribute = new();
            attribute.Read(reader);
            attributes.Add(attribute);
        }

        return attributes;
    }

    public static void WriteList(BinaryWriter writer, IReadOnlyList<Attribute> attributes)
    {
        writer.WriteVarInt(attributes.Count);
        for (int i = 0; i < attributes.Count; i++)
        {
            attributes[i].Write(writer);
        }
    }
}
