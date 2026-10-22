using BasaltBE.Nbt.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Nbt.Types;

public abstract class Tag {
    public string Name = string.Empty;
    public abstract TagType Type { get; }

    /// <summary>Returns the encoded size of this tag, including its type and optional name.</summary>
    public int GetSize(Options? options = null) {
        if (Type == TagType.End)
            return sizeof(byte);

        options ??= Options.LittleEndian;
        return checked(sizeof(byte) + (options.HasName ? Nbt.GetStringSize(Name, options) : 0) + GetPayloadSize(options));
    }

    internal abstract void Read(ref BinaryReader reader, Options options);
    internal abstract void Write(ref BinaryWriter writer, Options options);
    internal abstract int GetPayloadSize(Options options);
}
