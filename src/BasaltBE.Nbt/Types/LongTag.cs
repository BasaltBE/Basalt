using BasaltBE.Nbt.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Nbt.Types;

public sealed class LongTag : Tag {
    public long Value;
    public override TagType Type => TagType.Long;

    internal override void Read(ref BinaryReader reader, Options options) =>
        Value = options.VarInt ? reader.ReadZigZong() : reader.ReadInt64(options.ByteOrder == ByteOrder.LittleEndian);

    internal override void Write(ref BinaryWriter writer, Options options) {
        if (options.VarInt) writer.WriteZigZong(Value);
        else writer.WriteInt64(Value, options.ByteOrder == ByteOrder.LittleEndian);
    }
}
