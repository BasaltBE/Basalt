namespace BasaltBE.Protocol.Enums;

public enum PacketCompressionAlgorithm : ushort {
    ZLib = 0,
    Snappy = 1,
    None = ushort.MaxValue
}
