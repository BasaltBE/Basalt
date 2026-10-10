using System.Net;

namespace BasaltBE.NetherNet.Types;

public sealed record NethernetServerOptions {
    public required ServerAdvertisement Advertisement { get; init; }
    public IPEndPoint HttpAddress { get; init; } = new(IPAddress.Any, 19132);
    public IPEndPoint DiscoveryAddress { get; init; } = new(IPAddress.Any, 7551);
    public string IdentityKeyPath { get; init; } = "nethernet-identity.pem";
    public System.Security.Cryptography.X509Certificates.X509Certificate2? TlsCertificate { get; init; }
    public int MaximumPeers { get; init; } = 64;
    public int MaximumQueuedMessages { get; init; } = 128;
    public int MaximumSdpLength { get; init; } = 1_048_576;
    public int ReliableFragmentLength { get; init; } = 262_143;
    public int UnreliableFragmentLength { get; init; } = 262_143;
    public int MaximumPacketLength { get; init; } = 4 * 1024 * 1024;
    public TimeSpan NegotiationTimeout { get; init; } = TimeSpan.FromSeconds(20);
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public ulong ApplicationId { get; init; } = 0xDEADBEEF;
    public ulong NetworkId { get; init; }
}

