namespace BasaltBE.NetherNet.Types;

public readonly record struct NethernetPeer(ulong NetworkId, ulong ConnectionId, System.Net.IPAddress? Address = null);
