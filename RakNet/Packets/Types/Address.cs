using Basalt.Binary;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Basalt.RakNet.Packets.Types;

internal static class SocketAddressUtils
{
    extension(SocketAddress address)
    {
        public ushort GetPort()
        {
            return (ushort)((address[2] << 8) | address[3]);
        }
        public void SetPort(ushort port)
        {
            address[2] = (byte)(port >> 8);
            address[3] = (byte)(port & 0xFF);
        }
        public Span<byte> GetIPv4AddressBytes()
        {
            return address.Buffer.Span[4..8];
        }
        public Span<byte> GetIPv6AddressBytes()
        {
            return address.Buffer.Span[8..(8 + 16)];
        }
        public Span<byte> GetScopeIdBytes()
        {
            return address.Buffer.Span[24..28];
        } 
        public Span<byte> GetFlowInformationBytes()
        {
            return address.Buffer.Span[4..8];
        }

        public Span<byte> GetFullIPv6Bytes()
        {
            return address.Buffer.Span[8..28];
        }
        public void SetFamily(AddressFamily family)
        {
            BinaryPrimitives.WriteUInt16BigEndian(address.Buffer.Span, (ushort)family);
        }
        // Is weak bc doesn't includes flow information,
        // bc flow information might change for single enpoint
        public int GetHashCodeWeak()
        {
            HashCode hash = new();

            hash.Add(address.Family);
            hash.Add(address.GetPort());

            if (address.Family == AddressFamily.InterNetwork)
                hash.AddBytes(address.GetIPv4AddressBytes());

            else if (address.Family == AddressFamily.InterNetworkV6)
            {
                hash.AddBytes(address.GetIPv4AddressBytes());
                hash.AddBytes(address.GetScopeIdBytes());
            }

            address.GetHashCode();

            return hash.ToHashCode();
        }
        public static SocketAddress Read(ReadOnlySpan<byte> src, ref int offset)
        {
            byte version = src.ReadUInt8(offset);
            offset += 1;

            if (version == 4)
            {
                SocketAddress add = new SocketAddress(AddressFamily.InterNetwork);
                MemoryMarshal.Write(add.GetIPv4AddressBytes(), ~MemoryMarshal.Read<int>(src[offset..]));
                offset += 4;
                add.SetPort(src.ReadUInt16(offset, false));
                offset += 2;
                return add;
            }

            if (version == 6)
            {
                SocketAddress add = new SocketAddress(AddressFamily.InterNetworkV6);
                _ = src.ReadUInt16(offset, true);
                offset += 2;
                add.SetPort(src.ReadUInt16(offset, false));
                offset += 2;

                Span<byte> dest = add.GetFullIPv6Bytes();
                src[offset..(offset + dest.Length)].CopyTo(dest);
                offset += 4;  // Flow info
                offset += 16; // ip6 bytes
                offset += 4;  // scopeId
                return add;
            }

            throw new InvalidOperationException($"Unsupported address version: {version}");
        }
        public void Write(Span<byte> dest, ref int offset)
        {
            dest.WriteUInt8(address.Family == AddressFamily.InterNetwork?(byte)4:(byte)6, offset);
            offset += 1;

            if (address.Family == AddressFamily.InterNetwork)
            {
                // Flip the bits
                MemoryMarshal.Write(dest[offset..], ~MemoryMarshal.Read<int>(address.GetIPv4AddressBytes()));
                offset += 4;
                dest.WriteUInt16(address.GetPort(), offset, false);
                offset += 2;
                return;
            }


            if (address.Family == AddressFamily.InterNetworkV6)
            {
                dest.WriteUInt16((ushort)AddressFamily.InterNetworkV6, offset, true);
                offset += 2;

                dest.WriteUInt16(address.GetPort(), offset, false);
                offset += 2;

                address.GetFullIPv6Bytes().CopyTo(dest[offset..]);
                offset += 4;  // Flow info
                offset += 16; // ip6 bytes
                offset += 4;  // scopeId
                return;
            }

            throw new InvalidOperationException("Invalid address version.");
        }
    }
}

/*
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
*/