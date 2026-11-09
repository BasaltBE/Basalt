using BasaltBE.Nbt.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Nbt.Types;

public sealed class ShortTag : Tag {
    public short Value;
    public override TagType Type => TagType.Short;
    internal override void Read(ref BinaryReader reader, Options options, ref NbtReadContext context) => Value = reader.ReadInt16(options.ByteOrder == ByteOrder.LittleEndian);
    internal override void Write(ref BinaryWriter writer, Options options) => writer.WriteInt16(Value, options.ByteOrder == ByteOrder.LittleEndian);
    internal override int GetPayloadSize(Options options) => sizeof(short);
}
