namespace BasaltBE.NetherNet.Types;

public sealed record NethernetClientOptions {
    public required Uri SignalingUri { get; init; }
    public int MaximumQueuedMessages { get; init; } = 128;
    public int MaximumSdpLength { get; init; } = 1_048_576;
    public int ReliableFragmentLength { get; init; } = 262_143;
    public int UnreliableFragmentLength { get; init; } = 262_143;
    public int MaximumPacketLength { get; init; } = 4 * 1024 * 1024;
    public TimeSpan NegotiationTimeout { get; init; } = TimeSpan.FromSeconds(20);
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);
}
