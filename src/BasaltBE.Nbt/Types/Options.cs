using BasaltBE.Nbt.Enums;

namespace BasaltBE.Nbt.Types;

public sealed class Options {
    public static Options LittleEndian { get; } = new();
    public static Options BigEndian { get; } = new() { ByteOrder = ByteOrder.BigEndian };

    public ByteOrder ByteOrder = ByteOrder.LittleEndian;
    public bool VarInt;
    public bool HasName = true;
    public bool StringLengthVarInt;
}
