using Basalt.Binary;

namespace Basalt.RakNet.Packets;

public struct OpenConnectionRequestOne(byte protocolVersion, ushort mtu)
{
    public const byte PacketId = 0x05;

    public byte ProtocolVersion = protocolVersion;
    public ushort MTU = mtu;


    public static OpenConnectionRequestOne Deserialize(ReadOnlySpan<byte> src)
    {
        return new(src.ReadInt64(1, false), src.ReadUInt64(1 + 8 + Magic.MAGIC_LENGTH, false));
    }

    public static int Serialize(OpenConnectionRequestOne ping, Span<byte> dest)
    {
        // Do we? the packet id?
        dest.WriteUInt8(PacketId);


        dest.WriteInt64(ping.Time, 1, true);
        Magic.Write(dest, 1 + 8);
        dest.WriteUInt64(ping.Guid, 1 + 8 + Magic.MAGIC_LENGTH, true);

        // Return the size written
        return 1 + 8 + Magic.MAGIC_LENGTH + 8;
    }
}
