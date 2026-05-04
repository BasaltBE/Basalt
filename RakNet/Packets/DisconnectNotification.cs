using Basalt.Binary;

namespace Basalt.RakNet.Packets;

public struct DisconnectNotification
{
    public const byte PacketId = 0x15;

    public static DisconnectNotification Deserialize(ReadOnlySpan<byte> src)
    {
        if (src.Length < 1 || src.ReadUInt8(0) != PacketId)
        {
            throw new InvalidOperationException("Invalid packet id.");
        }

        return new();
    }

    public static int Serialize(DisconnectNotification packet, Span<byte> dest)
    {
        dest.WriteUInt8(PacketId, 0);
        return 1;
    }
}
