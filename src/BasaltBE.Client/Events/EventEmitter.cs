using Basalt.Binary;
using BasaltBE.Protocol.Packets;
using BinaryReader = Basalt.Binary.BinaryReader;

namespace BasaltBE.Client.Events;

public delegate void RawPacketHandler(uint packetId, ReadOnlySpan<byte> payload);

public sealed class EventEmitter {
    private readonly Dictionary<uint, IPacketDispatcher> dispatchers = [];
    private RawPacketHandler[] rawSubscribers = [];

    public void Subscribe<TPacket>(PacketEvent<TPacket> packetEvent, Action<TPacket> subscriber)
        where TPacket : DataPacket, new() {
        ArgumentNullException.ThrowIfNull(packetEvent);
        ArgumentNullException.ThrowIfNull(subscriber);

        if (!dispatchers.TryGetValue(packetEvent.PacketId, out IPacketDispatcher? dispatcher)) {
            dispatcher = new PacketDispatcher<TPacket>();
            dispatchers.Add(packetEvent.PacketId, dispatcher);
        }

        if (dispatcher is not PacketDispatcher<TPacket> typedDispatcher) {
            throw new InvalidOperationException(
                $"Packet ID 0x{packetEvent.PacketId:X} is already registered for another packet type."
            );
        }

        typedDispatcher.Subscribe(subscriber);
    }

    public void SubscribeRaw(RawPacketHandler subscriber) {
        ArgumentNullException.ThrowIfNull(subscriber);
        rawSubscribers = [.. rawSubscribers, subscriber];
    }

    internal void Emit(uint packetId, ReadOnlySpan<byte> payload) {
        if (dispatchers.TryGetValue(packetId, out IPacketDispatcher? dispatcher)) {
            dispatcher.Emit(packetId, payload);
            return;
        }

        foreach (RawPacketHandler subscriber in rawSubscribers) {
            subscriber(packetId, payload);
        }
    }

    private interface IPacketDispatcher {
        void Emit(uint packetId, ReadOnlySpan<byte> payload);
    }

    private sealed class PacketDispatcher<TPacket> : IPacketDispatcher where TPacket : DataPacket, new() {
        private Action<TPacket>[] subscribers = [];

        public void Subscribe(Action<TPacket> subscriber) => subscribers = [.. subscribers, subscriber];

        public void Emit(uint packetId, ReadOnlySpan<byte> payload) {
            TPacket packet = new();
            int offset = 0;
            BinaryReader reader = new(payload, ref offset);
            packet.Deserialize(ref reader);
            if (offset != payload.Length) {
                throw new InvalidDataException($"Bedrock packet 0x{packetId:X} has trailing data.");
            }

            foreach (Action<TPacket> subscriber in subscribers) {
                subscriber(packet);
            }
        }
    }
}
