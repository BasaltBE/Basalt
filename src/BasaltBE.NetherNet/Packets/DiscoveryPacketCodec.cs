using System.Security.Cryptography;
using System.Text;
using Basalt.Binary;
using BasaltBE.NetherNet.Enums;

namespace BasaltBE.NetherNet.Packets;

public static class DiscoveryPacketCodec {
    public const int MaximumDatagramLength = 65_507;
    private const int MacLength = 32;
    private const int BlockLength = 16;
    private const int HeaderLength = 18;
    private static readonly byte[] ApplicationKey = CreateKey(0xDEADBEEF);

    public static byte[] Encode(DiscoveryPacket packet, ulong applicationId = 0xDEADBEEF) {
        ArgumentNullException.ThrowIfNull(packet);
        int contentLength = packet.Type switch {
            DiscoveryPacketType.Request => 0,
            DiscoveryPacketType.Response => checked(packet.Data.Length * 2 + sizeof(uint)),
            DiscoveryPacketType.Message => checked(packet.Data.Length + sizeof(ulong) + sizeof(uint)),
            _ => throw new ArgumentOutOfRangeException(nameof(packet))
        };
        int plainLength = checked(sizeof(ushort) + HeaderLength + contentLength);
        int paddedLength = checked((plainLength / BlockLength + 1) * BlockLength);
        if (MacLength + paddedLength > MaximumDatagramLength || plainLength > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(packet), "Discovery packet is too large.");

        byte[] plaintext = new byte[paddedLength];
        using BinaryStream stream = new(plaintext.AsMemory(0, plainLength));
        Basalt.Binary.BinaryWriter writer = stream.GetWriter();
        writer.WriteUInt16((ushort)plainLength, littleEndian: true);
        writer.WriteUInt16((ushort)packet.Type, littleEndian: true);
        writer.WriteUInt64(packet.SenderId, littleEndian: true);
        writer.WriteUInt64(0, littleEndian: true);
        if (packet.Type == DiscoveryPacketType.Response) {
            string hex = Convert.ToHexString(packet.Data);
            byte[] encodedHex = Encoding.ASCII.GetBytes(hex);
            writer.WriteUInt32((uint)encodedHex.Length, littleEndian: true);
            writer.WriteBytes(encodedHex);
        }
        else if (packet.Type == DiscoveryPacketType.Message) {
            writer.WriteUInt64(packet.RecipientId, littleEndian: true);
            writer.WriteUInt32((uint)packet.Data.Length, littleEndian: true);
            writer.WriteBytes(packet.Data);
        }

        plaintext.AsSpan(plainLength).Fill((byte)(paddedLength - plainLength));
        byte[] output = new byte[MacLength + paddedLength];
        byte[] key = applicationId == 0xDEADBEEF ? ApplicationKey : CreateKey(applicationId);
        HMACSHA256.HashData(key, plaintext.AsSpan(0, plainLength), output.AsSpan(0, MacLength));
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.EncryptEcb(plaintext, output.AsSpan(MacLength), PaddingMode.None);
        return output;
    }

    public static bool TryDecode(ReadOnlySpan<byte> datagram, out DiscoveryPacket? packet, ulong applicationId = 0xDEADBEEF) {
        packet = null;
        if (datagram.Length < MacLength + BlockLength || datagram.Length > MaximumDatagramLength ||
            (datagram.Length - MacLength) % BlockLength != 0)
            return false;

        byte[] plaintext = new byte[datagram.Length - MacLength];
        byte[] key = applicationId == 0xDEADBEEF ? ApplicationKey : CreateKey(applicationId);
        using (Aes aes = Aes.Create()) {
            aes.Key = key;
            aes.DecryptEcb(datagram[MacLength..], plaintext, PaddingMode.None);
        }
        int padding = plaintext[^1];
        if (padding is < 1 or > BlockLength || padding > plaintext.Length ||
            !IsUniform(plaintext.AsSpan(plaintext.Length - padding), (byte)padding))
            return false;

        int plainLength = plaintext.Length - padding;
        ReadOnlySpan<byte> body = plaintext.AsSpan(0, plainLength);
        Span<byte> expectedMac = stackalloc byte[MacLength];
        HMACSHA256.HashData(key, body, expectedMac);
        if (!CryptographicOperations.FixedTimeEquals(expectedMac, datagram[..MacLength]) || body.Length < sizeof(ushort) + HeaderLength)
            return false;

        try {
            int offset = 0;
            Basalt.Binary.BinaryReader reader = new(body, ref offset);
            if (reader.ReadUInt16(littleEndian: true) != body.Length) return false;
            ushort rawType = reader.ReadUInt16(littleEndian: true);
            if (rawType > (ushort)DiscoveryPacketType.Message) return false;
            DiscoveryPacketType type = (DiscoveryPacketType)rawType;
            ulong senderId = reader.ReadUInt64(littleEndian: true);
            reader.ReadUInt64(littleEndian: true);
            ReadOnlySpan<byte> content = reader.GetRemainingBytes();
            ulong recipientId = 0;
            byte[] data = [];

            if (type == DiscoveryPacketType.Response) {
                if (content.Length < sizeof(uint)) return false;
                offset = 0;
                Basalt.Binary.BinaryReader contentReader = new(content, ref offset);
                uint hexLength = contentReader.ReadUInt32(littleEndian: true);
                if (hexLength != contentReader.Remaining || (hexLength & 1) != 0) return false;
                data = Convert.FromHexString(Encoding.ASCII.GetString(contentReader.ReadBytes((int)hexLength)));
            }
            else if (type == DiscoveryPacketType.Message) {
                if (content.Length < sizeof(ulong) + sizeof(uint)) return false;
                offset = 0;
                Basalt.Binary.BinaryReader contentReader = new(content, ref offset);
                recipientId = contentReader.ReadUInt64(littleEndian: true);
                uint length = contentReader.ReadUInt32(littleEndian: true);
                if (length != contentReader.Remaining) return false;
                data = contentReader.ReadBytes((int)length).ToArray();
            }
            else if (!content.IsEmpty) {
                return false;
            }

            packet = new DiscoveryPacket(type, senderId, recipientId, data);
            return true;
        }
        catch (ArgumentOutOfRangeException) {
            return false;
        }
        catch (FormatException) {
            return false;
        }
    }

    private static byte[] CreateKey(ulong applicationId) {
        Span<byte> id = stackalloc byte[sizeof(ulong)];
        int offset = 0;
        Basalt.Binary.BinaryWriter writer = new(id, ref offset);
        writer.WriteUInt64(applicationId, littleEndian: true);
        return SHA256.HashData(id);
    }

    private static bool IsUniform(ReadOnlySpan<byte> value, byte expected) {
        foreach (byte item in value)
            if (item != expected) return false;
        return true;
    }
}

