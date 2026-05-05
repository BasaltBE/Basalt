using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public static class UUID
{
    public static Guid Read(ref BinaryReader reader)
    {
        Span<byte> uuidBytes = stackalloc byte[16];
        reader.ReadBytes(16).CopyTo(uuidBytes);
        uuidBytes[..8].Reverse();
        uuidBytes[8..].Reverse();
        return new Guid(uuidBytes);
    }

    public static void Write(ref BinaryWriter writer, Guid value)
    {
        byte[] uuidBytes = value.ToByteArray();
        uuidBytes[..8].Reverse();
        uuidBytes[8..].Reverse();
        writer.WriteBytes(uuidBytes);
    }
}
