using BasaltBE.Nbt.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Nbt.Types;

public sealed class IntArrayTag : Tag {
    public int[] Value = [];
    public override TagType Type => TagType.IntArray;

    internal override void Read(ref BinaryReader reader, Options options) {
        Value = new int[Nbt.ReadLength(ref reader, options)];
        for (int i = 0; i < Value.Length; i++)
            Value[i] = options.VarInt ? reader.ReadZigZag() : reader.ReadInt32(options.ByteOrder == ByteOrder.LittleEndian);
    }

    internal override void Write(ref BinaryWriter writer, Options options) {
        Nbt.WriteLength(ref writer, Value.Length, options);
        foreach (int value in Value) {
            if (options.VarInt) writer.WriteZigZag(value);
            else writer.WriteInt32(value, options.ByteOrder == ByteOrder.LittleEndian);
        }
    }

    internal override int GetPayloadSize(Options options) {
        int size = Nbt.GetLengthSize(Value.Length, options);
        foreach (int value in Value)
            size = checked(size + (options.VarInt ? Nbt.GetVarIntSize(value) : sizeof(int)));
        return size;
    }
}
