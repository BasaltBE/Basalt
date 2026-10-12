using BasaltBE.Protocol.Enums;

namespace BasaltBE.Client;

public readonly record struct BedrockNetworkSettings(
    ushort CompressionThreshold,
    PacketCompressionAlgorithm CompressionAlgorithm
);
