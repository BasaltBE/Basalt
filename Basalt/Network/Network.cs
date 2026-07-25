namespace Basalt.Core.Network;

using System.Buffers;
using System.Collections.Concurrent;
using Basalt.Binary;
using Basalt.Core.Events;
using Basalt.Core.Network.Handlers;
using Basalt.Core.Profiling;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.RakNet;
using Basalt.RakNet.Packets.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;



public sealed class NetworkHandler {
    private const int MaxPacketBatchSize = 1024 * 1024 * 8;
    private const int MaxPacketSize = 1024 * 1024 * 4;

    private readonly Server _server;
    private readonly ConcurrentQueue<(NetworkConnection Connection, byte[] Payload)> _incomingFrames = new();
    private readonly ConcurrentQueue<IncomingPacket> _incomingPackets = new();
    private readonly ConcurrentQueue<NetworkConnection> _disconnections = new();
    private readonly Dictionary<Type, List<PacketListener>> _packetListeners = [];
    private readonly object _packetListenersLock = new();
    private readonly ConcurrentQueue<QueuedOutgoing> _outgoingPackets = new();
    private readonly Dictionary<NetworkConnection, List<OutgoingPacket>> _outgoingBuffer = [];
    private readonly Stack<List<OutgoingPacket>> _outgoingLists = [];
    private int _threadId;

    public NetworkHandler(Server server) {
        _server = server;
        On<LoginPacket>((connection, packet) => Login.Handle(_server, connection, packet));
        On<RequestNetworkSettingsPacket>((connection, packet) => RequestNetworkSettings.Handle(_server, connection, packet));
        On<ResourcePackClientResponsePacket>((connection, packet) => ResourcePackClientResponse.Handle(_server, connection, packet));
        On<ResourcePackChunkRequestPacket>((connection, packet) => ResourcePackChunkRequest.Handle(_server, connection, packet));
        On<RequestChunkRadiusPacket>((connection, packet) => RequestChunkRadius.Handle(_server, connection, packet));
        On<SetLocalPlayerAsInitializedPacket>((connection, packet) => SetLocalPlayerAsInitialized.Handle(_server, connection, packet));
        On<PlayerAuthInputPacket>((connection, packet) => PlayerAuthInput.Handle(_server, connection, packet));
        On<InteractPacket>((connection, packet) => Interact.Handle(_server, connection, packet));
        On<AnimatePacket>((connection, packet) => Animate.Handle(_server, connection, packet));
        On<ContainerClosePacket>((connection, packet) => ContainerClose.Handle(_server, connection, packet));
        On<InventoryTransactionPacket>((connection, packet) => InventoryTransaction.Handle(_server, connection, packet));
        On<MobEquipmentPacket>((connection, packet) => MobEquipment.Handle(_server, connection, packet));
        On<PlayerActionPacket>((connection, packet) => PlayerAction.Handle(_server, connection, packet));
        On<RespawnPacket>((connection, packet) => Respawn.Handle(_server, connection, packet));
        On<ItemStackRequestPacket>((connection, packet) => ItemStackRequest.Handle(_server, connection, packet));
        On<ClientCacheStatusPacket>((connection, packet) => ClientCacheStatus.Handle(_server, connection, packet));
        On<CommandRequestPacket>((connection, packet) => CommandRequest.Handle(_server, connection, packet));
        On<TextPacket>((connection, packet) => Text.Handle(_server, connection, packet));
        On<ModalFormResponsePacket>((connection, packet) => ModalFormResponse.Handle(_server, connection, packet));
        On<ServerboundDataStorePacket>((connection, packet) => ServerboundDataStore.Handle(_server, connection, packet));
    }

    /// <summary>
    /// Adds a typed packet listener that runs on the main server thread.
    /// </summary>
    public void On<TPacket>(Action<NetworkConnection, TPacket> listener) where TPacket : DataPacket {
        ArgumentNullException.ThrowIfNull(listener);
        lock (_packetListenersLock) {
            Type packetType = typeof(TPacket);
            if (!_packetListeners.TryGetValue(packetType, out List<PacketListener>? listeners)) {
                listeners = [];
                _packetListeners[packetType] = listeners;
            }

            listeners.Add(new PacketListener<TPacket>(listener));
        }
    }

    /// <summary>
    /// Removes a typed packet listener.
    /// </summary>
    public void Off<TPacket>(Action<NetworkConnection, TPacket> listener) where TPacket : DataPacket {
        ArgumentNullException.ThrowIfNull(listener);
        lock (_packetListenersLock) {
            if (!_packetListeners.TryGetValue(typeof(TPacket), out List<PacketListener>? listeners)) {
                return;
            }

            listeners.RemoveAll(current => current.Matches(listener));
            if (listeners.Count == 0) {
                _packetListeners.Remove(typeof(TPacket));
            }
        }
    }

    internal void EnqueueFrame(NetworkConnection connection, ReadOnlyMemory<byte> payload) {
        _incomingFrames.Enqueue((connection, payload.ToArray()));
    }

    internal void EnqueueDisconnection(NetworkConnection connection) {
        _disconnections.Enqueue(connection);
    }

    internal void Tick() {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Network.Tick") : default;
        _threadId = Environment.CurrentManagedThreadId;

        while (_incomingFrames.TryDequeue(out var frame)) {
            DeserializeFrame(frame.Connection, frame.Payload);
        }

        FlushOutgoing();
    }

    internal void ProcessIncoming() {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Network.ProcessIncoming") : default;

        using (Profiler.Enabled ? Profiler.BeginZone("Network.ProcessDisconnections") : default) {
            while (_disconnections.TryDequeue(out NetworkConnection? connection)) {
                try {
                    HandleDisconnected(connection);
                }
                catch (Exception exception) {
                    Logger.Warn($"Unhandled disconnect error: {exception}");
                }
            }
        }

        using (Profiler.Enabled ? Profiler.BeginZone("Network.ProcessPackets") : default) {
            while (_incomingPackets.TryDequeue(out var packet)) {
                DispatchPacket(packet);
            }
        }
    }

    public void HandleDisconnected(NetworkConnection connection) {
        if (!_server.Players.TryRemove(connection, out Player.Player? player)) {
            return;
        }

        PlayerAuthInput.Cleanup(player.RuntimeId);

        Entities.Traits.Types.EntityDespawnOptions options = new(Disconnected: true);
        _server.Emit(new PlayerLeaveSignal(player, options));

        Worlds.World world = _server.GetWorld();
        world.Persistence.SavePlayerData(player.Xuid, player.Write());


        string leaveMessage = $"§e{player.Username} left the server.";
        foreach (Player.Player target in _server.Players.Values) {
            target.SendMessage(leaveMessage);
        }

        if (player.IsAlive && player.Dimension is not null) {
            player.Despawn(options);
        }

        PlayerListPacket removePlayer = new() {
            ActionType = PlayerListActionType.Remove,
            Entries =
            [
                new Basalt.Protocol.Types.PlayerListEntry
                {
                    Uuid = player.Uuid
                }
            ]
        };
        _server.Broadcast(removePlayer);

        Logger.Info($"Player {player.Username} disconnected.");
    }

    private void DeserializeFrame(NetworkConnection connection, ReadOnlyMemory<byte> payload) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Network.DeserializeFrame") : default;
        ReadOnlySpan<byte> packetData = payload.Span;
        byte[]? decompressedBuffer = null;

        try {
            decompressedBuffer = ArrayPool<byte>.Shared.Rent(MaxPacketSize);
            int decompressedLength;
            using (Profiler.Enabled ? Profiler.BeginZone("Network.Unframe") : default) {
                decompressedLength = Protocol.Io.Packet.Unframe(packetData, decompressedBuffer, out _);
            }
            if (decompressedLength == 0) return;

            ReadOnlySpan<byte> frame = decompressedBuffer.AsSpan(0, decompressedLength);

            int offset = 0;
            BinaryReader frameReader = new(frame, ref offset);

            using (Profiler.Enabled ? Profiler.BeginZone("Network.DispatchPackets") : default) {
                while (frameReader.Remaining > 0) {
                    int packetLength = checked((int)frameReader.ReadVarUInt());
                    if (packetLength <= 0 || packetLength > frameReader.Remaining) break;

                    ReadOnlySpan<byte> packetBuffer = frameReader.ReadBytes(packetLength);
                    if (packetBuffer.Length == 0) continue;


                    try {
                        int offset2 = 0;
                        BinaryReader packetReader = new(packetBuffer, ref offset2);
                        DataPacket packet = Protocol.Io.Packet.Deserialize(packetReader);
                        _incomingPackets.Enqueue(new IncomingPacket(
                            connection,
                            packet,
                            Protocol.Io.Packet.GetId(packet),
                            _server.HasListeners(ServerEvent.PacketReceive)
                                ? packetBuffer.ToArray()
                                : ReadOnlyMemory<byte>.Empty));
                    }
                    catch (Exception exception) {
                        Logger.Warn($"Packet decode error ({packetBuffer.Length} bytes): {exception}");
                    }
                }
            }
        }
        catch (Exception exception) {
            Logger.Warn($"Network frame error: {exception}");
        }
        finally {
            if (decompressedBuffer is not null) {
                ArrayPool<byte>.Shared.Return(decompressedBuffer);
            }
        }
    }

    private void DispatchPacket(IncomingPacket incoming) {
        if (_server.HasListeners(ServerEvent.PacketReceive)) {
            _server.Players.TryGetValue(incoming.Connection, out Player.Player? packetPlayer);
            PacketReceiveSignal receiveSignal = new(
                incoming.Connection,
                packetPlayer,
                incoming.PacketId,
                incoming.PacketBuffer,
                incoming.Packet);
            _server.Emit(receiveSignal);
            if (receiveSignal.Cancelled) {
                return;
            }
        }

        PacketListener[] listeners;
        lock (_packetListenersLock) {
            if (!_packetListeners.TryGetValue(incoming.Packet.GetType(), out List<PacketListener>? registered)) {
                return;
            }

            listeners = [.. registered];
        }

        for (int i = 0; i < listeners.Length; i++) {
            listeners[i].Invoke(incoming.Connection, incoming.Packet);
        }
    }

    public void QueuePacket(NetworkConnection connection, DataPacket packet, CompressionMethod? compression = null) {
        if (_server.HasListeners(ServerEvent.PacketSend)) {
            _server.Players.TryGetValue(connection, out Player.Player? player);
            PacketSendSignal sendSignal = new(connection, player, packet);
            _server.Emit(sendSignal);
            if (sendSignal.Cancelled) {
                return;
            }
        }

        QueuePackets(connection, [packet], compression);
    }

    public void SendPacket(NetworkConnection connection, DataPacket packet, CompressionMethod? compression = null) {
        if (_server.HasListeners(ServerEvent.PacketSend)) {
            _server.Players.TryGetValue(connection, out Player.Player? player);
            PacketSendSignal sendSignal = new(connection, player, packet);
            _server.Emit(sendSignal);
            if (sendSignal.Cancelled) {
                return;
            }
        }

        SendPackets(connection, [packet], compression);
    }

    public void QueueSerializedPacket(
        NetworkConnection connection,
        PacketId packetId,
        ReadOnlySpan<byte> packetPayload,
        CompressionMethod? compression = null) {
        using BinaryStream packetBufferStream = BinaryStream.Rent(packetPayload.Length + 16);

        BinaryWriter packetWriter = packetBufferStream;
        packetWriter.WriteVarInt((int)packetId);
        packetWriter.WriteBytes(packetPayload);

        QueueOutgoing(
            connection,
            null,
            packetWriter.GetProcessedBytes(),
            compression,
            false,
            false);
    }

    public void SendSerializedPacket(
        NetworkConnection connection,
        PacketId packetId,
        ReadOnlySpan<byte> packetPayload,
        CompressionMethod? compression = null) {
        using BinaryStream packetBufferStream = BinaryStream.Rent(packetPayload.Length + 16);

        BinaryWriter packetWriter = packetBufferStream;
        packetWriter.WriteVarInt((int)packetId);
        packetWriter.WriteBytes(packetPayload);

        QueueOutgoing(
            connection,
            null,
            packetWriter.GetProcessedBytes(),
            compression,
            true,
            true);
    }

    public void SendSerializedPackets(
        NetworkConnection connection,
        ReadOnlySpan<(PacketId Id, byte[] Payload)> packets,
        CompressionMethod? compression = null) {
        using BinaryStream packetBufferStream = BinaryStream.Rent(MaxPacketSize);

        foreach ((PacketId id, byte[] payload) in packets) {
            packetBufferStream.Offset = 0;
            BinaryWriter packetWriter = packetBufferStream;
            packetWriter.WriteVarInt((int)id);
            packetWriter.WriteBytes(payload);

            QueueOutgoing(
                connection,
                null,
                packetWriter.GetProcessedBytes(),
                compression,
                false,
                false);
        }
    }

    public void SendPackets(NetworkConnection connection, IEnumerable<DataPacket> packets, CompressionMethod? compression = null) {
        QueuePackets(connection, packets, compression, true, true);
    }

    public void QueuePackets(NetworkConnection connection, IEnumerable<DataPacket> packets, CompressionMethod? compression = null) {
        QueuePackets(connection, packets, compression, false, false);
    }

    private void QueuePackets(
        NetworkConnection connection,
        IEnumerable<DataPacket> packets,
        CompressionMethod? compression,
        bool immediate,
        bool wait) {
        using IEnumerator<DataPacket> enumerator = packets.GetEnumerator();

        if (!enumerator.MoveNext()) {
            return;
        }

        while (true) {
            DataPacket packet = enumerator.Current;
            bool last = !enumerator.MoveNext();
            QueueOutgoing(
                connection,
                packet,
                [],
                compression,
                immediate && last,
                wait && last);
            if (last) {
                return;
            }
        }
    }

    private void QueueOutgoing(
        NetworkConnection connection,
        DataPacket? packet,
        ReadOnlySpan<byte> serialized,
        CompressionMethod? compression,
        bool immediate,
        bool wait) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Network.QueueOutgoing") : default;
        TaskCompletionSource? completion = wait
            ? new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
            : null;
        byte[]? payload = null;
        if (!serialized.IsEmpty) {
            using (Profiler.Enabled ? Profiler.BeginZone("Network.CopyPayload") : default) {
                payload = ArrayPool<byte>.Shared.Rent(serialized.Length);
                serialized.CopyTo(payload);
            }
        }

        using (Profiler.Enabled ? Profiler.BeginZone("Network.EnqueueOutgoing") : default) {
            _outgoingPackets.Enqueue(new QueuedOutgoing(
                connection,
                new OutgoingPacket(
                    packet,
                    payload,
                    serialized.Length,
                    compression,
                    immediate,
                    completion)));
        }

        if (wait) {
            if (Environment.CurrentManagedThreadId == _threadId) {
                FlushOutgoing();
            }

            using (Profiler.Enabled ? Profiler.BeginZone("Network.WaitForSend") : default) {
                completion!.Task.GetAwaiter().GetResult();
            }
        }
    }

    private void FlushOutgoing() {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Network.FlushOutgoing") : default;
        if (!_outgoingPackets.TryDequeue(out QueuedOutgoing queued)) {
            return;
        }

        Dictionary<NetworkConnection, List<OutgoingPacket>> outgoing = _outgoingBuffer;
        int remaining = _outgoingPackets.Count;
        using (Profiler.Enabled ? Profiler.BeginZone("Network.TakeOutgoing") : default) {
            do {
                if (!outgoing.TryGetValue(queued.Connection, out List<OutgoingPacket>? packets)) {
                    packets = _outgoingLists.Count > 0 ? _outgoingLists.Pop() : [];
                    outgoing[queued.Connection] = packets;
                }

                packets.Add(queued.Packet);
            }
            while (remaining-- > 0 && _outgoingPackets.TryDequeue(out queued));
        }

        Exception? failure = null;
        try {
            foreach ((NetworkConnection connection, List<OutgoingPacket> packets) in outgoing) {
                Send(connection, packets);
            }
        }
        catch (Exception exception) {
            failure = exception;
            throw;
        }
        finally {
            foreach (List<OutgoingPacket> packets in outgoing.Values) {
                for (int i = 0; i < packets.Count; i++) {
                    OutgoingPacket packet = packets[i];
                    if (packet.Payload is not null) {
                        ArrayPool<byte>.Shared.Return(packet.Payload);
                    }
                    if (failure is null) {
                        packet.Completion?.TrySetResult();
                    }
                    else {
                        packet.Completion?.TrySetException(failure);
                    }
                }

                packets.Clear();
            }

            foreach (List<OutgoingPacket> packets in outgoing.Values) {
                _outgoingLists.Push(packets);
            }
            outgoing.Clear();
        }
    }

    private void Send(NetworkConnection connection, List<OutgoingPacket> packets) {
        SerializedOutgoing[] serialized = ArrayPool<SerializedOutgoing>.Shared.Rent(packets.Count);
        int count = 0;

        try {
            using BinaryStream packetStream = BinaryStream.Rent(MaxPacketSize);
            for (; count < packets.Count; count++) {
                OutgoingPacket outgoing = packets[count];
                if (outgoing.Payload is not null) {
                    serialized[count] = new SerializedOutgoing(
                        outgoing.Payload,
                        outgoing.Length,
                        outgoing.Compression,
                        outgoing.Immediate);
                    continue;
                }

                packetStream.Offset = 0;
                BinaryWriter writer = packetStream;
                Protocol.Io.Packet.Serialize(outgoing.Packet!, writer);
                ReadOnlySpan<byte> packet = writer.GetProcessedBytes();
                byte[] payload = ArrayPool<byte>.Shared.Rent(packet.Length);
                packet.CopyTo(payload);
                serialized[count] = new SerializedOutgoing(
                    payload,
                    packet.Length,
                    outgoing.Compression,
                    outgoing.Immediate);
            }

            int start = 0;
            while (start < count) {
                start = SendBatch(connection, serialized, start, count);
            }
        }
        finally {
            for (int i = 0; i < count; i++) {
                if (packets[i].Packet is not null) {
                    ArrayPool<byte>.Shared.Return(serialized[i].Payload);
                }
            }

            serialized.AsSpan(0, count).Clear();
            ArrayPool<SerializedOutgoing>.Shared.Return(serialized);
        }
    }

    private int SendBatch(
        NetworkConnection connection,
        SerializedOutgoing[] packets,
        int start,
        int count) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Network.SendBatch") : default;
        CompressionMethod? compression = packets[start].Compression;
        int end = start;
        int frameCapacity = 0;
        bool immediate = false;

        while (end < count && packets[end].Compression == compression) {
            int packetCapacity = packets[end].Length + 5;
            if (frameCapacity != 0 && frameCapacity + packetCapacity > MaxPacketBatchSize) {
                break;
            }

            frameCapacity += packetCapacity;
            immediate |= packets[end].Immediate;
            end++;
        }

        int packetCount = end - start;
        ReadOnlyMemory<byte>[] payloads = ArrayPool<ReadOnlyMemory<byte>>.Shared.Rent(packetCount);
        byte[] frame = ArrayPool<byte>.Shared.Rent(frameCapacity);

        try {
            using (Profiler.Enabled ? Profiler.BeginZone("Network.BuildBatch") : default) {
                for (int i = 0; i < packetCount; i++) {
                    SerializedOutgoing packet = packets[start + i];
                    payloads[i] = packet.Payload.AsMemory(0, packet.Length);
                }
            }

            int frameLength;
            using (Profiler.Enabled ? Profiler.BeginZone("Network.FrameBatch") : default) {
                frameLength = Protocol.Io.Packet.Frame(payloads.AsSpan(0, packetCount), frame);
            }
            SendFrame(connection, frame.AsSpan(0, frameLength), compression, immediate);
            return end;
        }
        finally {
            payloads.AsSpan(0, packetCount).Clear();
            ArrayPool<ReadOnlyMemory<byte>>.Shared.Return(payloads);
            ArrayPool<byte>.Shared.Return(frame);
        }
    }

    private void SendFrame(NetworkConnection connection, ReadOnlySpan<byte> frame, CompressionMethod? compression, bool immediate) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Network.SendFrame") : default;
        CompressionMethod method = compression ?? GetCompressionMethod(_server.Properties.CompressionMethod);
        byte[] compressedBuffer = ArrayPool<byte>.Shared.Rent(
            Protocol.Io.Packet.GetFrameCapacity(frame.Length, method));

        try {
            int frameLength;
            using (Profiler.Enabled ? Profiler.BeginZone("Network.CompressFrame") : default) {
                frameLength = Protocol.Io.Packet.Frame(
                    frame,
                    compressedBuffer,
                    method,
                    _server.Properties.CompressionThreshold);
            }
            using (Profiler.Enabled ? Profiler.BeginZone("Network.RakNetSend") : default) {
                connection.SendPacket(compressedBuffer.AsSpan(0, frameLength), Reliability.ReliableOrdered, immediate);
            }
        }
        finally {
            ArrayPool<byte>.Shared.Return(compressedBuffer);
        }
    }

    private readonly record struct OutgoingPacket(
        DataPacket? Packet,
        byte[]? Payload,
        int Length,
        CompressionMethod? Compression,
        bool Immediate,
        TaskCompletionSource? Completion);

    private readonly record struct QueuedOutgoing(
        NetworkConnection Connection,
        OutgoingPacket Packet);

    private readonly record struct SerializedOutgoing(
        byte[] Payload,
        int Length,
        CompressionMethod? Compression,
        bool Immediate);

    private readonly record struct IncomingPacket(
        NetworkConnection Connection,
        DataPacket Packet,
        PacketId PacketId,
        ReadOnlyMemory<byte> PacketBuffer);

    private abstract class PacketListener {
        public abstract void Invoke(NetworkConnection connection, DataPacket packet);
        public abstract bool Matches(Delegate listener);
    }

    private sealed class PacketListener<TPacket> : PacketListener where TPacket : DataPacket {
        private readonly Action<NetworkConnection, TPacket> _listener;

        public PacketListener(Action<NetworkConnection, TPacket> listener) {
            _listener = listener;
        }

        public override void Invoke(NetworkConnection connection, DataPacket packet) {
            _listener(connection, (TPacket)packet);
        }

        public override bool Matches(Delegate listener) {
            return _listener == (Action<NetworkConnection, TPacket>)listener;
        }
    }

    private static CompressionMethod GetCompressionMethod(string? value) {
        if (value is not null && value.Equals("snappy", StringComparison.OrdinalIgnoreCase)) {
            return CompressionMethod.Snappy;
        }

        return CompressionMethod.Zlib;
    }

}










