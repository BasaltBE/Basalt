namespace BasaltBE.NetherNet.Types;

public readonly record struct NethernetMessage(
    NethernetPeer Peer,
    Enums.MessageReliability Reliability,
    ReadOnlyMemory<byte> Payload,
    bool Connected = false,
    bool Disconnected = false);

