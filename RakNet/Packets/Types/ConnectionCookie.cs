using System.Net;
using System.Security.Cryptography;

namespace Basalt.RakNet.Packets.Types;

public static class ConnectionCookie
{
    public static uint Create(IPEndPoint endpoint, ReadOnlySpan<byte> secret, uint? window = null)
    {
        uint Window = window ?? (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);

        Span<byte> Input = stackalloc byte[24];
        int offset = 0;

        byte[] AddressBytes = endpoint.Address.GetAddressBytes();
        Input[offset++] = (byte)AddressBytes.Length;
        AddressBytes.CopyTo(Input[offset..]);
        offset += AddressBytes.Length;

        Input[offset++] = (byte)(endpoint.Port >> 8);
        Input[offset++] = (byte)endpoint.Port;

        Input[offset++] = (byte)(Window >> 24);
        Input[offset++] = (byte)(Window >> 16);
        Input[offset++] = (byte)(Window >> 8);
        Input[offset++] = (byte)Window;

        byte[] Hash = HMACSHA256.HashData(secret, Input[..offset]);
        return ((uint)Hash[0] << 24) | ((uint)Hash[1] << 16) | ((uint)Hash[2] << 8) | Hash[3];
    }

    public static bool Validate(IPEndPoint endpoint, ReadOnlySpan<byte> secret, uint cookie)
    {
        uint Current = Create(endpoint, secret);
        if (cookie == Current)
        {
            return true;
        }

        uint PreviousWindow = (uint)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30) - 1;
        uint Previous = Create(endpoint, secret, PreviousWindow);
        return cookie == Previous;
    }
}
