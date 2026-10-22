using BasaltBE.Nbt.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Nbt.Types;

public sealed class StringTag : Tag {
    public string Value = string.Empty;
    public override TagType Type => TagType.String;
    internal override void Read(ref BinaryReader reader, Options options) => Value = Nbt.ReadString(ref reader, options);
    internal override void Write(ref BinaryWriter writer, Options options) => Nbt.WriteString(ref writer, Value, options);
    internal override int GetPayloadSize(Options options) => Nbt.GetStringSize(Value, options);
}
