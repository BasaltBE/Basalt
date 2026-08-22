using Basalt.Binary;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.BedrockProtocol.Types;

public sealed class AttributeData : DataType {
    public float Minimum;
    public float Maximum;
    public float Current;
    public float DefaultMinimum;
    public float DefaultMaximum;
    public float Default;
    public string Name = string.Empty;
    public AttributeModifier[] Modifiers = [];

    public override void Write(ref BinaryWriter writer) {
        writer.WriteF32(Minimum, true);
        writer.WriteF32(Maximum, true);
        writer.WriteF32(Current, true);
        writer.WriteF32(DefaultMinimum, true);
        writer.WriteF32(DefaultMaximum, true);
        writer.WriteF32(Default, true);
        writer.WriteVarString(Name);
        writer.WriteVarUInt((uint)Modifiers.Length);
        for (int i = 0; i < Modifiers.Length; i++) Modifiers[i].Write(ref writer);
    }

    public override void Read(ref BinaryReader reader) {
        Minimum = reader.ReadF32(true);
        Maximum = reader.ReadF32(true);
        Current = reader.ReadF32(true);
        DefaultMinimum = reader.ReadF32(true);
        DefaultMaximum = reader.ReadF32(true);
        Default = reader.ReadF32(true);
        Name = reader.ReadVarString();
        int count = checked((int)reader.ReadVarUInt());
        Modifiers = new AttributeModifier[count];
        for (int i = 0; i < count; i++) {
            Modifiers[i] = new AttributeModifier();
            Modifiers[i].Read(ref reader);
        }
    }
}
