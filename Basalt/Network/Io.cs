namespace Basalt.Core.Network;

using Basalt.BedrockProtocol.Types;

public class NetworkIo {
    public static Uuid FromGuid(Guid guid) {
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes, bigEndian: true, out _);

        return new Uuid {
            MostSignificantBits = System.Buffers.Binary.BinaryPrimitives
                .ReadUInt64BigEndian(bytes[..8]),

            LeastSignificantBits = System.Buffers.Binary.BinaryPrimitives
                .ReadUInt64BigEndian(bytes[8..])
        };
    }
}
