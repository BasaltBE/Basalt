using BasaltBE.Nbt.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Nbt.Types;

public abstract class Tag {
    public string Name = string.Empty;
    public abstract TagType Type { get; }

    internal abstract void Read(ref BinaryReader reader, Options options);
    internal abstract void Write(ref BinaryWriter writer, Options options);
}
