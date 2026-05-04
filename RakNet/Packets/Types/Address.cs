using Basalt.Binary;
using System.Net;
using System.Net.Sockets;

namespace Basalt.RakNet.Packets.Types;


// I aint commenting this, too lazy
public struct Address(byte version = 4, byte[]? ip = null, ushort port = 0)
{
    public byte Version = version;
    public byte[] Ip = ip ?? new byte[16];
    public ushort Port = port;

    public static Address Read(ReadOnlySpan<byte> src, out int bytesRead, int offset = 0)
    {
        int startOffset = offset;

        byte Version = src.ReadUInt8(offset);
        offset += 1;

        byte[] Ip = new byte[16];

        if (Version == 4)
        {
            for (int Index = 0; Index < 4; Index++)
            {
                Ip[Index] = (byte)~src.ReadUInt8(offset);
                offset += 1;
            }

            ushort Port = src.ReadUInt16(offset, false);
            offset += 2;

            bytesRead = offset - startOffset;
            return new(Version, Ip, Port);
        }

        if (Version == 6)
        {
            _ = src.ReadUInt16(offset, true);
            offset += 2;

            ushort Port = src.ReadUInt16(offset, false);
            offset += 2;

            _ = src.ReadUInt32(offset, true);
            offset += 4;

            src.Slice(offset, 16).CopyTo(Ip);
            offset += 16;

            _ = src.ReadUInt32(offset, true);
            offset += 4;

            bytesRead = offset - startOffset;
            return new(Version, Ip, Port);
        }

        throw new InvalidOperationException("Invalid address version.");
    }

    public static int Write(Address address, Span<byte> dest, int offset = 0)
    {
        int startOffset = offset;

        dest.WriteUInt8(address.Version, offset);
        offset += 1;

        if (address.Version == 4)
        {
            for (int Index = 0; Index < 4; Index++)
            {
                dest.WriteUInt8((byte)~address.Ip[Index], offset);
                offset += 1;
            }

            dest.WriteUInt16(address.Port, offset, false);
            offset += 2;

            return offset - startOffset;
        }

        if (address.Version == 6)
        {
            dest.WriteUInt16(23, offset, true);
            offset += 2;

            dest.WriteUInt16(address.Port, offset, false);
            offset += 2;

            dest.WriteUInt32(0, offset, true);
            offset += 4;

            address.Ip.AsSpan(0, 16).CopyTo(dest[offset..]);
            offset += 16;

            dest.WriteUInt32(0, offset, true);
            offset += 4;

            return offset - startOffset;
        }

        throw new InvalidOperationException("Invalid address version.");
    }

    public static Address FromEndPoint(EndPoint endpoint)
    {
        if (endpoint is not IPEndPoint IpEndPoint)
        {
            return new(4, new byte[16], 0);
        }

        if (IpEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return new(6, IpEndPoint.Address.GetAddressBytes(), (ushort)IpEndPoint.Port);
        }

        byte[] Ip = new byte[16];
        byte[] SourceIp = IpEndPoint.Address.GetAddressBytes();
        Ip[0] = SourceIp[0];
        Ip[1] = SourceIp[1];
        Ip[2] = SourceIp[2];
        Ip[3] = SourceIp[3];
        return new(4, Ip, (ushort)IpEndPoint.Port);
    }
}
