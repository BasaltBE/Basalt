using Basalt.Protocol.Enums;

namespace Basalt.Core;

/// <summary>
/// Configuration options for the Basalt server.
/// 
/// THIS IS MAINLY FOR NOW!! As we will use server.properties or sum later on
/// </summary>
public readonly record struct ServerOptions
{
    public ServerOptions()
    {
        CompressionMethod = CompressionMethod.Zlib;
        CompressionThreshold = 1;
        MaxPlayers = 100;
        WorldProvider = "leveldb";
        WorldPath = "worlds/world";
        DefaultWorldIdentifier = "world";
        Mtu = 1024;
        OfflineMode = false;
    }

    /// <summary>
    /// The compression algorithm used for outgoing packets.
    /// </summary>
    public CompressionMethod CompressionMethod { get; init; }

    /// <summary>
    /// The minimum payload size (in bytes) before compression is applied.
    /// Packets smaller than this threshold are sent uncompressed.
    /// </summary>
    public ushort CompressionThreshold { get; init; }

    /// <summary>
    /// The maximum number of players that can be connected to the server simultaneously.
    /// </summary>
    public int MaxPlayers { get; init; }

    public string WorldProvider { get; init; }

    public string WorldPath { get; init; }

    public string DefaultWorldIdentifier { get; init; }

    /// <summary>
    /// The maximum transmission unit (MTU) for outgoing packets.
    /// </summary>
    public ushort Mtu { get; init; }

    /// <summary>
    /// When true, clients without Xbox Live may join using a self-signed offline certificate.
    /// When false, offline certificates are rejected.
    /// </summary>
    public bool OfflineMode { get; init; }
}
