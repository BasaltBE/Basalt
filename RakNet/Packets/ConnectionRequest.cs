using Basalt.Binary;

namespace Basalt.RakNet.Packets;

public struct ConnectionRequest(
    ulong clientGuid = 0,
    ulong clientSendTime = 0,
    bool doSecurity = false,
    byte[]? clientProof = null,
    bool doIdentity = false,
    byte[]? identityProof = null
)
{
    public const byte PacketId = 0x09;

    public ulong ClientGuid = clientGuid;
    public ulong ClientSendTime = clientSendTime;
    public bool DoSecurity = doSecurity;
    public byte[] ClientProof = clientProof ?? new byte[32];
    public bool DoIdentity = doIdentity;
    public byte[] IdentityProof = identityProof ?? new byte[294];

    public static ConnectionRequest Deserialize(ReadOnlySpan<byte> src)
    {
        if (src.Length < 18)
        {
            throw new InvalidOperationException("Invalid ConnectionRequest length.");
        }

        int offset = 0;
        byte packetId = src.ReadUInt8(offset);
        offset += 1;

        if (packetId != PacketId)
        {
            throw new InvalidOperationException("Invalid packet id.");
        }

        ulong clientGuid = src.ReadUInt64(offset, false);
        offset += 8;

        ulong clientSendTime = src.ReadUInt64(offset, false);
        offset += 8;

        bool doSecurity = src.ReadBool(offset);
        offset += 1;

        if (src.Length < offset + 32 + 1 + 294)
        {
            throw new InvalidOperationException("Invalid ConnectionRequest payload length.");
        }

        byte[] clientProof = src.Slice(offset, 32).ToArray();
        offset += 32;

        bool doIdentity = src.ReadBool(offset);
        offset += 1;

        byte[] identityProof = src.Slice(offset, 294).ToArray();
        return new(clientGuid, clientSendTime, doSecurity, clientProof, doIdentity, identityProof);
    }

    public static int Serialize(ConnectionRequest packet, Span<byte> dest)
    {
        int offset = 0;
        dest.WriteUInt8(PacketId, offset);
        offset += 1;

        dest.WriteUInt64(packet.ClientGuid, offset, false);
        offset += 8;

        dest.WriteUInt64(packet.ClientSendTime, offset, false);
        offset += 8;

        dest.WriteBool(packet.DoSecurity, offset);
        offset += 1;

        packet.ClientProof.AsSpan(0, Math.Min(packet.ClientProof.Length, 32)).CopyTo(dest[offset..]);
        if (packet.ClientProof.Length < 32)
        {
            dest.Slice(offset + packet.ClientProof.Length, 32 - packet.ClientProof.Length).Clear();
        }
        offset += 32;

        dest.WriteBool(packet.DoIdentity, offset);
        offset += 1;

        packet.IdentityProof.AsSpan(0, Math.Min(packet.IdentityProof.Length, 294)).CopyTo(dest[offset..]);
        if (packet.IdentityProof.Length < 294)
        {
            dest.Slice(offset + packet.IdentityProof.Length, 294 - packet.IdentityProof.Length).Clear();
        }
        offset += 294;

        return offset;
    }
}
