using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Basalt.Binary;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Profiling;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Packets;
using BinaryReader = Basalt.Binary.BinaryReader;

namespace BasaltBE.Basalt.Network.Events;

public sealed class EventEmitter {
    private readonly Dictionary<uint, IPacketDispatcher> dispatchers = [];

    public void Subscribe<TPacket>(
        PacketEvent<TPacket> packetEvent,
        Action<NethernetPeer, Player?, TPacket> subscriber
    ) where TPacket : DataPacket, new() {
        ArgumentNullException.ThrowIfNull(packetEvent);
        ArgumentNullException.ThrowIfNull(subscriber);
        if (!dispatchers.TryGetValue(packetEvent.PacketId, out IPacketDispatcher? dispatcher)) {
            dispatcher = new PacketDispatcher<TPacket>();
            dispatchers.Add(packetEvent.PacketId, dispatcher);
        }
        if (dispatcher is not PacketDispatcher<TPacket> typedDispatcher)
            throw new InvalidOperationException($"Packet ID 0x{packetEvent.PacketId:X} is already registered for another packet type.");
        typedDispatcher.Subscribe(subscriber);
    }

    internal bool Emit(uint packetId, NethernetPeer peer, Player? player, ReadOnlyMemory<byte> payload) {
        using ProfilerZone zone = Profiler.BeginZone("Network.Dispatch");
        if (!dispatchers.TryGetValue(packetId, out IPacketDispatcher? dispatcher)) return false;
        dispatcher.Emit(packetId, peer, player, payload);
        return true;
    }

    private interface IPacketDispatcher {
        void Emit(uint packetId, NethernetPeer peer, Player? player, ReadOnlyMemory<byte> payload);
    }

    private sealed class PacketDispatcher<TPacket> : IPacketDispatcher where TPacket : DataPacket, new() {
        private Action<NethernetPeer, Player?, TPacket>[] subscribers = [];

        public void Subscribe(Action<NethernetPeer, Player?, TPacket> subscriber) =>
            subscribers = [.. subscribers, subscriber];

        public void Emit(uint packetId, NethernetPeer peer, Player? player, ReadOnlyMemory<byte> payload) {
            TPacket packet = new();
            using (Profiler.BeginZone("Network.DeserializePacket")) {
                int offset = 0;
                BinaryReader reader = new(payload.Span, ref offset);
                packet.Deserialize(ref reader);
                if (offset != payload.Length)
                    throw new InvalidDataException($"Bedrock packet 0x{packetId:X} has trailing data.");
            }

            try {
                using (Profiler.BeginZone("Network.HandlePacket")) {
                    foreach (Action<NethernetPeer, Player?, TPacket> subscriber in subscribers)
                        subscriber(peer, player, packet);
                }
            }
            finally {
                if (packet is LoginPacket login && MemoryMarshal.TryGetArray(login.ConnectionRequest, out ArraySegment<byte> request))
                    CryptographicOperations.ZeroMemory(request.AsSpan());
            }
        }
    }
}
