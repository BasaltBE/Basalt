using BasaltBE.Nbt.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Nbt.Types;

public sealed class LongArrayTag : Tag {
    public long[] Value = [];
    public override TagType Type => TagType.LongArray;

    internal override void Read(ref BinaryReader reader, Options options, ref NbtReadContext context) {
        int count = Nbt.ReadLength(ref reader, options);
        context.CheckArrayLength(count);
        if (count > reader.Remaining / (options.VarInt ? 1 : sizeof(long)))
            throw new InvalidDataException("NBT long array length exceeds the remaining data.");
        Value = new long[count];
        for (int i = 0; i < Value.Length; i++)
            Value[i] = options.VarInt ? reader.ReadZigZong() : reader.ReadInt64(options.ByteOrder == ByteOrder.LittleEndian);
    }

    internal override void Write(ref BinaryWriter writer, Options options) {
        Nbt.WriteLength(ref writer, Value.Length, options);
        foreach (long value in Value) {
            if (options.VarInt) writer.WriteZigZong(value);
            else writer.WriteInt64(value, options.ByteOrder == ByteOrder.LittleEndian);
        }
    }

    internal override int GetPayloadSize(Options options) {
        int size = Nbt.GetLengthSize(Value.Length, options);
        foreach (long value in Value)
            size = checked(size + (options.VarInt ? Nbt.GetVarIntSize(value) : sizeof(long)));
        return size;
    }
}
