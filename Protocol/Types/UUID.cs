using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Protocol.Types;

public static class UUID
{
    public static Guid Read(BinaryReader reader)
    {
        byte[] uuidBytes = new byte[16];
        reader.ReadBytes(16).CopyTo(uuidBytes);
        uuidBytes[..8].Reverse();
        uuidBytes[8..].Reverse();
        return new Guid($"{Convert.ToHexString(uuidBytes[..4])}-{Convert.ToHexString(uuidBytes[4..6])}-{Convert.ToHexString(uuidBytes[6..8])}-{Convert.ToHexString(uuidBytes[8..10])}-{Convert.ToHexString(uuidBytes[10..])}");
    }

    public static void Write(BinaryWriter writer, Guid value)
    {
        byte[] uuidBytes = new byte[16];
        string text = value.ToString("N");
        for (int i = 0; i < uuidBytes.Length; i++)
        {
            uuidBytes[i] = Convert.ToByte(text.Substring(i * 2, 2), 16);
        }

        uuidBytes[..8].Reverse();
        uuidBytes[8..].Reverse();
        writer.WriteBytes(uuidBytes);
    }
}
