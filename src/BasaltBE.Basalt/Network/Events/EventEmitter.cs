using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Basalt.Binary;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Logging;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Packets;
using BinaryReader = Basalt.Binary.BinaryReader;

namespace BasaltBE.Basalt.Network.Events;

public sealed class EventEmitter {
    private readonly ConcurrentDictionary<uint, IPacketDispatcher> dispatchers = [];

    public void Subscribe<TPacket>(
        PacketEvent<TPacket> packetEvent,
        Action<NethernetPeer, Player?, TPacket> subscriber
    ) where TPacket : DataPacket, new() {
        ArgumentNullException.ThrowIfNull(packetEvent);
        ArgumentNullException.ThrowIfNull(subscriber);
        IPacketDispatcher dispatcher = dispatchers.GetOrAdd(packetEvent.PacketId, static _ => new PacketDispatcher<TPacket>());
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
        private readonly Lock sync = new();
        private Action<NethernetPeer, Player?, TPacket>[] subscribers = [];

        public void Subscribe(Action<NethernetPeer, Player?, TPacket> subscriber) {
            lock (sync)
                Volatile.Write(ref subscribers, [.. subscribers, subscriber]);
        }

        public void Emit(uint packetId, NethernetPeer peer, Player? player, ReadOnlyMemory<byte> payload) {
            TPacket packet = new();
            try {
                using (Profiler.BeginZone("Network.DeserializePacket")) {
                    int offset = 0;
                    BinaryReader reader = new(payload.Span, ref offset);
                    packet.Deserialize(ref reader);
                    if (offset != payload.Length)
                        throw new InvalidDataException($"Bedrock packet 0x{packetId:X} has trailing data.");
                }
            }
            catch (Exception error) when (
                packetId == PacketEvents.ItemStackRequest.PacketId &&
                error is IndexOutOfRangeException or InvalidDataException or ArgumentOutOfRangeException or OverflowException or FormatException) {
                int length = Math.Min(payload.Length, 256);
                string prefix = Convert.ToHexString(payload.Span[..length]);
                string truncated = payload.Length > length ? " (truncated)" : string.Empty;
                Logger.Scoped("Network").Warn(
                    $"Could not decode item stack request from {peer.ConnectionId}: {error.Message} " +
                    $"({payload.Length} bytes, payload: {prefix}{truncated})."
                );
                return;
            }

            try {
                using (Profiler.BeginZone("Network.HandlePacket")) {
                    foreach (Action<NethernetPeer, Player?, TPacket> subscriber in Volatile.Read(ref subscribers))
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
