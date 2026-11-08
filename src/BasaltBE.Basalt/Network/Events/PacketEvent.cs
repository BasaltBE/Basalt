using System.Reflection;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt.Network.Events;

public sealed class PacketEvent<TPacket> where TPacket : DataPacket, new() {
    public readonly uint PacketId;

    public PacketEvent() => PacketId = GetPacketId();

    public PacketEvent(uint packetId) => PacketId = packetId;

    private static uint GetPacketId() {
        PacketIdAttribute? attribute = typeof(TPacket).GetCustomAttribute<PacketIdAttribute>();
        if (attribute is null) throw new InvalidOperationException($"{typeof(TPacket).Name} has no packet ID attribute.");
        return checked((uint)attribute.Id);
    }
}
