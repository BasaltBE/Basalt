using System.Buffers.Binary;
using System.Net;
using System.Security.Cryptography;

namespace Basalt.RakNet.Packets.Types;

public static class ConnectionCookie
{
    public static uint Create(SocketAddress address, ReadOnlySpan<byte> secret, uint? window = null)
    {
        uint _win = window ?? (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);

        Span<byte> input = stackalloc byte[24];
        int offset = 0;

        ReadOnlySpan<byte> addressBytes = address.Family == System.Net.Sockets.AddressFamily.InterNetwork ?
            address.GetIPv4AddressBytes() : address.GetIPv6AddressBytes();

        input[offset++] = (byte)addressBytes.Length;
        addressBytes.CopyTo(input[offset..]);
        offset += addressBytes.Length;

        BinaryPrimitives.WriteUInt16BigEndian(input[offset..], address.GetPort());
        offset += 2;
        /*
        input[offset++] = (byte)(endpoint.Port >> 8);
        input[offset++] = (byte)endpoint.Port;*/
        /*
        input[offset++] = (byte)(_win >> 24);
        input[offset++] = (byte)(_win >> 16);
        input[offset++] = (byte)(_win >> 8);
        input[offset++] = (byte)_win;*/


        BinaryPrimitives.WriteUInt32BigEndian(input[offset..], _win);
        offset += 4;


        byte[] hash = HMACSHA256.HashData(secret, input[..offset]);
        return ((uint)hash[0] << 24) | ((uint)hash[1] << 16) | ((uint)hash[2] << 8) | hash[3];
    }

    public static bool Validate(SocketAddress address, ReadOnlySpan<byte> secret, uint cookie)
    {
        uint current = Create(address, secret);
        if (cookie == current)
        {
            return true;
        }

        uint previousWindow = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30) - 1;
        uint previous = Create(address, secret, previousWindow);
        return cookie == previous;
    }
}
