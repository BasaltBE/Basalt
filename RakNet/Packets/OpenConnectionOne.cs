using Basalt.Binary;
using Basalt.RakNet.Packets.Types;

namespace Basalt.RakNet.Packets;

public struct OpenConnectionRequestOne(byte protocolVersion, ushort mtu)
{
    public const byte PacketId = 0x05;

    public byte ProtocolVersion = protocolVersion;
    public ushort MTU = mtu;


    public static OpenConnectionRequestOne Deserialize(ReadOnlySpan<byte> src)
    {
        int offset = 1;
        offset += Magic.MAGIC_LENGTH;

        byte ProtocolVersion = src.ReadUInt8(offset);
        offset += 1;

        ushort MTU = src.ReadUInt16(offset, false);
        return new(ProtocolVersion, MTU);
    }

    public static int Serialize(OpenConnectionRequestOne packet, Span<byte> dest)
    {
        int offset = 0;
        dest.WriteUInt8(PacketId, offset);
        offset += 1;

        Magic.Write(dest, offset);
        offset += Magic.MAGIC_LENGTH;

        dest.WriteUInt8(packet.ProtocolVersion, offset);
        offset += 1;

        dest.WriteUInt16(packet.MTU, offset, false);
        offset += 2;

        return offset;
    }
}
