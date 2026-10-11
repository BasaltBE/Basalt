using BasaltBE.Nbt.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Nbt.Types;

public sealed class ByteArrayTag : Tag {
    public byte[] Value = [];
    public override TagType Type => TagType.ByteArray;
    internal override void Read(ref BinaryReader reader, Options options) => Value = Nbt.ReadBytes(ref reader, Nbt.ReadLength(ref reader, options));
    internal override void Write(ref BinaryWriter writer, Options options) {
        Nbt.WriteLength(ref writer, Value.Length, options);
        writer.WriteBytes(Value);
    }
}
