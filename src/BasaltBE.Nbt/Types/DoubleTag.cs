using BasaltBE.Nbt.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Nbt.Types;

public sealed class DoubleTag : Tag {
    public double Value;
    public override TagType Type => TagType.Double;
    internal override void Read(ref BinaryReader reader, Options options) => Value = reader.ReadF64(options.ByteOrder == ByteOrder.LittleEndian);
    internal override void Write(ref BinaryWriter writer, Options options) => writer.WriteF64(Value, options.ByteOrder == ByteOrder.LittleEndian);
    internal override int GetPayloadSize(Options options) => sizeof(double);
}
