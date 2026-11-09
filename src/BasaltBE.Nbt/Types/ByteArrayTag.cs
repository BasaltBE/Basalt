using BasaltBE.Nbt.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Nbt.Types;

public sealed class ByteArrayTag : Tag {
    public byte[] Value = [];
    public override TagType Type => TagType.ByteArray;
    internal override void Read(ref BinaryReader reader, Options options, ref NbtReadContext context) {
        int count = Nbt.ReadLength(ref reader, options);
        context.CheckArrayLength(count);
        if (count > reader.Remaining)
            throw new InvalidDataException("NBT byte array length exceeds the remaining data.");
        Value = Nbt.ReadBytes(ref reader, count);
    }
    internal override void Write(ref BinaryWriter writer, Options options) {
        Nbt.WriteLength(ref writer, Value.Length, options);
        writer.WriteBytes(Value);
    }

    internal override int GetPayloadSize(Options options) => checked(Nbt.GetLengthSize(Value.Length, options) + Value.Length);
}
