using BasaltBE.Nbt.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Nbt.Types;

public sealed class IntTag : Tag {
    public int Value;
    public override TagType Type => TagType.Int;

    internal override void Read(ref BinaryReader reader, Options options) =>
        Value = options.VarInt ? reader.ReadZigZag() : reader.ReadInt32(options.ByteOrder == ByteOrder.LittleEndian);

    internal override void Write(ref BinaryWriter writer, Options options) {
        if (options.VarInt) writer.WriteZigZag(Value);
        else writer.WriteInt32(Value, options.ByteOrder == ByteOrder.LittleEndian);
    }

    internal override int GetPayloadSize(Options options) => options.VarInt ? Nbt.GetVarIntSize(Value) : sizeof(int);
}
