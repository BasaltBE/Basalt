using BasaltBE.NetherNet.Enums;

namespace BasaltBE.NetherNet.Packets;

public sealed record DiscoveryPacket(DiscoveryPacketType Type, ulong SenderId, ulong RecipientId, byte[] Data);
