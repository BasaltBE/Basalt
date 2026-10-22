using BasaltBE.Nbt.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Nbt.Types;

public sealed class FloatTag : Tag {
    public float Value;
    public override TagType Type => TagType.Float;
    internal override void Read(ref BinaryReader reader, Options options) => Value = reader.ReadF32(options.ByteOrder == ByteOrder.LittleEndian);
    internal override void Write(ref BinaryWriter writer, Options options) => writer.WriteF32(Value, options.ByteOrder == ByteOrder.LittleEndian);
    internal override int GetPayloadSize(Options options) => sizeof(float);
}
