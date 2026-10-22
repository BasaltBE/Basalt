using BasaltBE.Nbt.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Nbt.Types;

public sealed class EndTag : Tag {
    public override TagType Type => TagType.End;
    internal override void Read(ref BinaryReader reader, Options options) { }
    internal override void Write(ref BinaryWriter writer, Options options) { }
    internal override int GetPayloadSize(Options options) => 0;
}
