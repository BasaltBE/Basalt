namespace Basalt.Protocol.Enums
{
    public enum CompressionMethod : byte
    {
        Zlib = 0,
        Snappy = 1,
        NotPresent = 2,
        None = 0xFF,
    }
}
