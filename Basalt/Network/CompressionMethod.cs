namespace Basalt.Core.Network;

public enum CompressionMethod : byte {
    Zlib = 0,
    Snappy = 1,
    NotPresent = 2,
    None = 0xFF
}
