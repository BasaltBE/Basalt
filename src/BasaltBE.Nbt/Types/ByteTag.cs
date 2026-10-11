using BasaltBE.Nbt.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Nbt.Types;

public sealed class ByteTag : Tag {
    public byte Value;
    public override TagType Type => TagType.Byte;
    internal override void Read(ref BinaryReader reader, Options options) => Value = reader.ReadUInt8();
    internal override void Write(ref BinaryWriter writer, Options options) => writer.WriteUInt8(Value);
}
