namespace Basalt.Core.Network;

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Basalt.Binary;
using Basalt.Core.Events;
using Basalt.Core.Network.Handlers;
using Basalt.Core.Profiling;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;



public sealed class NetworkHandler {
    private const int MaxPacketSize = 1024 * 1024 * 4;
    internal const int MaxIncomingFramesPerTick = 256;
    internal const int MaxIncomingPacketsPerTick = 2048;
    internal const int MaxOutgoingPacketsPerTick = 4096;
    private const int MaxPriorityIncomingPacketsPerTick = MaxIncomingPacketsPerTick / 2;

    private readonly Server _server;
    private readonly ConcurrentQueue<(NetworkConnection Connection, byte[] Payload)> _incomingFrames = new();
    private readonly ConcurrentQueue<IncomingPacket> _incomingPackets = new();
    private readonly ConcurrentQueue<IncomingPacket> _priorityIncomingPackets = new();
    private readonly ConcurrentQueue<NetworkConnection> _disconnections = new();
    private readonly ConcurrentQueue<NetworkConnection> _pendingDisconnects = new();
    private readonly ConcurrentQueue<NetworkConnection> _readyDisconnects = new();
    private readonly Dictionary<Type, List<PacketListener>> _packetListeners = [];
    private readonly object _packetListenersLock = new();
    private readonly ConcurrentQueue<QueuedOutgoing> _priorityOutgoingPackets = new();
    private readonly ConcurrentQueue<QueuedOutgoing> _outgoingPackets = new();
    private readonly ConcurrentQueue<QueuedOutgoing> _lowPriorityOutgoingPackets = new();
    private readonly Dictionary<NetworkConnection, List<OutgoingPacket>> _outgoingBuffer = [];
    private readonly Stack<List<OutgoingPacket>> _outgoingLists = [];
    private readonly AutoResetEvent _wake = new(false);
    private static readonly ConcurrentDictionary<Type, int> _generatedPacketIds = new();
    private Thread? _networkThread;
    private int _threadId;
    private long _sentBytes;
    private long _sentPackets;
    private long _sentFrames;
    private long _incomingQueueWaitTicks;
    private long _incomingQueueWaitCount;

    public int PendingIncomingFrameCount => _incomingFrames.Count;
    public int PendingIncomingPacketCount => _incomingPackets.Count + _priorityIncomingPackets.Count;
    public int PendingOutgoingPacketCount =>
        _priorityOutgoingPackets.Count + _outgoingPackets.Count + _lowPriorityOutgoingPackets.Count;
    public long SentBytes => Interlocked.Read(ref _sentBytes);
    public long SentPackets => Interlocked.Read(ref _sentPackets);
    public long SentFrames => Interlocked.Read(ref _sentFrames);
    public double AverageIncomingQueueWaitMilliseconds =>
        Interlocked.Read(ref _incomingQueueWaitCount) == 0
            ? 0
            : Interlocked.Read(ref _incomingQueueWaitTicks) * 1000.0 /
              Stopwatch.Frequency /
              Interlocked.Read(ref _incomingQueueWaitCount);
    internal WaitHandle WakeHandle => _wake;

    public NetworkHandler(Server server) {
        _server = server;
        /// Packets ported to new Protocol
        On<RequestNetworkSettingsPacket>((connection, packet) => RequestNetworkSettings.Handle(_server, connection, packet));
        On<LoginPacket>((connection, packet) => LoginHandler.Handle(_server, connection, packet));
        On<ResourcePackClientResponsePacket>((connection, packet) => ResourcePackClientResponse.Handle(_server, connection, packet));
        On<AnimatePacket>((connection, packet) => Animate.Handle(_server, connection, packet));
        On<PlayerActionPacket>((connection, packet) => PlayerAction.Handle(_server, connection, packet));
        On<InteractPacket>((connection, packet) => Interact.Handle(_server, connection, packet));
        On<RespawnPacket>((connection, packet) => Respawn.Handle(_server, connection, packet));
        On<RequestChunkRadiusPacket>((connection, packet) => RequestChunkRadius.Handle(_server, connection, packet));
        On<ResourcePackChunkRequestPacket>((connection, packet) => ResourcePackChunkRequest.Handle(_server, connection, packet));
        On<TextPacket>((connection, packet) => Text.Handle(_server, connection, packet));
        On<PlayerSkinPacket>((connection, packet) => PlayerSkin.Handle(_server, connection, packet));
        On<ServerboundDataStorePacket>((connection, packet) => ServerboundDataStore.Handle(_server, connection, packet));
        On<ContainerClosePacket>((connection, packet) => ContainerClose.Handle(_server, connection, packet));
        On<ModalFormResponsePacket>((connection, packet) => ModalFormResponse.Handle(_server, connection, packet));
        On<ClientCacheStatusPacket>((connection, packet) => ClientCacheStatus.Handle(_server, connection, packet));
        On<PacketViolationWarningPacket>((connection, packet) => PacketViolationWarning.Handle(_server, connection, packet));
        On<MobEquipmentPacket>((connection, packet) => MobEquipment.Handle(_server, connection, packet));
        On<CommandRequestPacket>((connection, packet) => CommandRequest.Handle(_server, connection, packet));
        On<SetLocalPlayerAsInitializedPacket>((connection, packet) => SetLocalPlayerAsInitialized.Handle(_server, connection, packet));
        On<PlayerAuthInputPacket>((connection, packet) => PlayerAuthInput.Handle(_server, connection, packet));
        On<ItemStackRequestPacket>((connection, packet) => ItemStackRequest.Handle(_server, connection, packet));
        On<InventoryTransactionPacket>((connection, packet) => InventoryTransaction.Handle(_server, connection, packet));

    }

    /// <summary>
    /// Adds a typed packet listener that runs on the main server thread.
    /// </summary>
    public void On<TPacket>(Action<NetworkConnection, TPacket> listener) where TPacket : Packet {
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
    public void Off<TPacket>(Action<NetworkConnection, TPacket> listener) where TPacket : Packet {
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
        if (MemoryMarshal.TryGetArray(payload, out ArraySegment<byte> segment) &&
            segment.Offset == 0 &&
            segment.Count == segment.Array!.Length) {
            _incomingFrames.Enqueue((connection, segment.Array));
        }
        else {
            _incomingFrames.Enqueue((connection, payload.ToArray()));
        }
        _wake.Set();
    }

    internal void EnqueueDisconnection(NetworkConnection connection) {
        _disconnections.Enqueue(connection);
        _wake.Set();
    }

    internal void Tick() {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Network.Tick") : default;
        _threadId = Environment.CurrentManagedThreadId;

        while (_readyDisconnects.TryDequeue(out NetworkConnection? connection)) {
            connection.Disconnect();
        }

        int frames = 0;
        while (frames < MaxIncomingFramesPerTick && _incomingFrames.TryDequeue(out var frame)) {
            DeserializeFrame(frame.Connection, frame.Payload);
            frames++;
        }

        FlushOutgoing();

        while (_pendingDisconnects.TryDequeue(out NetworkConnection? connection)) {
            _readyDisconnects.Enqueue(connection);
        }
    }

    internal void Disconnect(NetworkConnection connection) {
        _pendingDisconnects.Enqueue(connection);
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
            int processed = 0;
            int priorityProcessed = 0;
            while (processed < MaxIncomingPacketsPerTick &&
                   TryDequeueIncoming(ref priorityProcessed, out IncomingPacket packet)) {
                Interlocked.Add(
                    ref _incomingQueueWaitTicks,
                    Stopwatch.GetTimestamp() - packet.QueuedTimestamp);
                Interlocked.Increment(ref _incomingQueueWaitCount);
                DispatchPacket(packet);
                processed++;
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

        _server.SavePlayer(player);


        string leaveMessage = $"§e{player.Username} left the server.";
        foreach (Player.Player target in _server.CurrentPlayersSnapshot) {
            target.SendMessage(leaveMessage);
        }

        if (player.IsAlive && player.Dimension is not null) {
            player.Despawn(options);
        }

        _server.Broadcast(
            new PlayerListPacket() {
                Action = PlayerListPacketType.Remove,
                RemoveEntries = [NetworkIo.FromGuid(player.Uuid)],
            }
        );

        Logger.Info($"Player {player.Username} disconnected.");
    }

    private void DeserializeFrame(NetworkConnection connection, ReadOnlyMemory<byte> payload) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Network.DeserializeFrame") : default;
        ReadOnlySpan<byte> packetData = payload.Span;
        byte[]? decompressedBuffer = null;

        try {
            ReadOnlySpan<byte> frame;
            if (!connection.NetherNetCompression) {
                frame = packetData;
            }
            else {
                if (packetData.Length == 0) {
                    return;
                }
                CompressionMethod compression = (CompressionMethod)packetData[0];
                ReadOnlySpan<byte> compressed = packetData[1..];
                if (compression == CompressionMethod.Zlib) {
                    decompressedBuffer = ArrayPool<byte>.Shared.Rent(MaxPacketSize);
                    int decompressedLength = PacketCompression.Decompress(compressed, decompressedBuffer);
                    frame = decompressedBuffer.AsSpan(0, decompressedLength);
                }
                else if (compression == CompressionMethod.None) {
                    frame = compressed;
                }
                else {
                    Logger.Warn($"Unsupported NetherNet compression method: {compression}");
                    return;
                }
            }

            int offset = 0;
            BinaryReader frameReader = new(frame, ref offset);

            using (Profiler.Enabled ? Profiler.BeginZone("Network.DispatchPackets") : default) {
                while (frameReader.Remaining > 0) {
                    int packetLength = checked((int)frameReader.ReadVarUInt());
                    if (packetLength <= 0 || packetLength > frameReader.Remaining) break;

                    ReadOnlySpan<byte> packetBuffer = frameReader.ReadBytes(packetLength);
                    if (packetBuffer.Length == 0) continue;


                    try {
                        if (!TryDeserializePacket(
                            packetBuffer,
                            out Packet? packet,
                            out int packetId)) {
                            continue;
                        }

                        IncomingPacket incoming = new(
                            connection,
                            packet!,
                            packetId,
                            Stopwatch.GetTimestamp(),
                            _server.HasListeners(ServerEvent.PacketReceive)
                                ? packetBuffer.ToArray()
                                : ReadOnlyMemory<byte>.Empty);
                        if (IsPriorityPacket(packet)) {
                            _priorityIncomingPackets.Enqueue(incoming);
                        }
                        else {
                            _incomingPackets.Enqueue(incoming);
                        }
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
        if (incoming.Packet is PlayerAuthInputPacket &&
            _server.Players.TryGetValue(incoming.Connection, out Player.Player? player)) {
            player.LastInputQueueWaitMilliseconds =
                (Stopwatch.GetTimestamp() - incoming.QueuedTimestamp) * 1000.0 / Stopwatch.Frequency;
        }

        if (_server.HasListeners(ServerEvent.PacketReceive)) {
            _server.Players.TryGetValue(incoming.Connection, out Player.Player? packetPlayer);
            PacketReceiveSignal receiveSignal = new(
                incoming.Connection,
                packetPlayer,
                incoming.Id,
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

    public void QueuePacket(
        NetworkConnection connection,
        Packet packet,
        CompressionMethod? compression = null) {
        ArgumentNullException.ThrowIfNull(packet);

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

    public void SendPacket(
        NetworkConnection connection,
        Packet packet,
        CompressionMethod? compression = null) {
        ArgumentNullException.ThrowIfNull(packet);

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
        int packetId,
        ReadOnlySpan<byte> packetPayload,
        CompressionMethod? compression = null) {
        using BinaryStream packetBufferStream = BinaryStream.Rent(packetPayload.Length + 16);

        BinaryWriter packetWriter = packetBufferStream;
        packetWriter.WriteVarUInt(checked((uint)packetId));
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
        int packetId,
        ReadOnlySpan<byte> packetPayload,
        CompressionMethod? compression = null) {
        using BinaryStream packetBufferStream = BinaryStream.Rent(packetPayload.Length + 16);

        BinaryWriter packetWriter = packetBufferStream;
        packetWriter.WriteVarUInt(checked((uint)packetId));
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
        ReadOnlySpan<(int Id, byte[] Payload)> packets,
        CompressionMethod? compression = null) {
        using BinaryStream packetBufferStream = BinaryStream.Rent(MaxPacketSize);

        foreach ((int id, byte[] payload) in packets) {
            packetBufferStream.Offset = 0;
            BinaryWriter packetWriter = packetBufferStream;
            packetWriter.WriteVarUInt(checked((uint)id));
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

    public void SendPackets(NetworkConnection connection, IEnumerable<Packet> packets, CompressionMethod? compression = null) {
        QueuePackets(connection, packets, compression, true, true);
    }

    public void QueuePackets(NetworkConnection connection, IEnumerable<Packet> packets, CompressionMethod? compression = null) {
        QueuePackets(connection, packets, compression, false, false);
    }

    private void QueuePackets(
        NetworkConnection connection,
        IEnumerable<Packet> packets,
        CompressionMethod? compression,
        bool immediate,
        bool wait) {
        ArgumentNullException.ThrowIfNull(packets);
        using IEnumerator<Packet> enumerator = packets.GetEnumerator();

        if (!enumerator.MoveNext()) {
            return;
        }

        while (true) {
            Packet packet = enumerator.Current
                ?? throw new ArgumentException("Packet collections cannot contain null values.", nameof(packets));

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
        Packet? packet,
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
            QueuedOutgoing queued = new(
                connection,
                new OutgoingPacket(
                    packet,
                    payload,
                    serialized.Length,
                    compression,
                    immediate,
                    completion));

            if (IsPriorityPacket(packet)) {
                _priorityOutgoingPackets.Enqueue(queued);
            }
            else if (IsLowPriorityPacket(packet)) {
                _lowPriorityOutgoingPackets.Enqueue(queued);
            }
            else {
                _outgoingPackets.Enqueue(queued);
            }
        }

        if (Environment.CurrentManagedThreadId != _threadId) {
            _wake.Set();
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
        if (!TryDequeueOutgoing(out QueuedOutgoing queued)) {
            return;
        }

        Dictionary<NetworkConnection, List<OutgoingPacket>> outgoing = _outgoingBuffer;
        int remaining = Math.Min(PendingOutgoingPacketCount, MaxOutgoingPacketsPerTick - 1);
        using (Profiler.Enabled ? Profiler.BeginZone("Network.TakeOutgoing") : default) {
            do {
                if (!outgoing.TryGetValue(queued.Connection, out List<OutgoingPacket>? packets)) {
                    packets = _outgoingLists.Count > 0 ? _outgoingLists.Pop() : [];
                    outgoing[queued.Connection] = packets;
                }

                packets.Add(queued.Packet);
            }
            while (remaining-- > 0 && TryDequeueOutgoing(out queued, remaining == 0));
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

    internal void Start(CancellationToken cancellationToken) {
        Thread networkThread = new(() => {
            Profiler.SetThreadName("Network");
            WaitHandle[] wakeHandles = [cancellationToken.WaitHandle, WakeHandle];
            while (!cancellationToken.IsCancellationRequested) {
                try {
                    Tick();
                }
                catch (Exception exception) {
                    Logger.Error($"Unhandled network tick error: {exception}");
                }

                if (PendingOutgoingPacketCount > 0 || PendingIncomingFrameCount > 0) {
                    continue;
                }

                WaitHandle.WaitAny(wakeHandles);
            }
        }) {
            Name = "Network",
            IsBackground = true
        };
        _networkThread = networkThread;
        networkThread.Start();
    }

    internal void Stop() {
        Thread? networkThread = _networkThread;
        _networkThread = null;
        networkThread?.Join(1000);
    }

    private bool TryDequeueIncoming(ref int priorityProcessed, out IncomingPacket packet) {
        if (priorityProcessed < MaxPriorityIncomingPacketsPerTick &&
            _priorityIncomingPackets.TryDequeue(out packet)) {
            priorityProcessed++;
            return true;
        }

        if (_incomingPackets.TryDequeue(out packet)) {
            return true;
        }

        if (_priorityIncomingPackets.TryDequeue(out packet)) {
            priorityProcessed++;
            return true;
        }

        return false;
    }

    private bool TryDequeueOutgoing(out QueuedOutgoing queued, bool reserveLowPriority = false) {
        if (reserveLowPriority && _lowPriorityOutgoingPackets.TryDequeue(out queued)) {
            return true;
        }

        if (_priorityOutgoingPackets.TryDequeue(out queued)) {
            return true;
        }

        if (_outgoingPackets.TryDequeue(out queued)) {
            return true;
        }

        return _lowPriorityOutgoingPackets.TryDequeue(out queued);
    }

    internal static bool IsPriorityPacket(Packet? packet) {
        return packet is PlayerAuthInputPacket
            or AddActorPacket
            or RemoveActorPacket
            or SetActorDataPacket
            or CommandOutputPacket
            or TextPacket;
    }

    internal static bool IsLowPriorityPacket(Packet? packet) {
        return packet is LevelChunkPacket
            or MoveActorAbsolutePacket
            or MoveActorDeltaPacket
            or SetActorMotionPacket;
    }

    internal void Dispose() {
        _wake.Dispose();
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
                        outgoing.Immediate,
                        IsUnreliable(outgoing.Packet));
                    continue;
                }

                packetStream.Offset = 0;
                BinaryWriter writer = packetStream;
                SerializeOutgoingPacket(outgoing.Packet!, writer);
                ReadOnlySpan<byte> packet = writer.GetProcessedBytes();

                byte[] payload = ArrayPool<byte>.Shared.Rent(packet.Length);
                packet.CopyTo(payload);
                serialized[count] = new SerializedOutgoing(
                    payload,
                    packet.Length,
                    outgoing.Compression,
                    outgoing.Immediate,
                    IsUnreliable(outgoing.Packet));
            }

            for (int index = 0; index < count; index++) {
                SendNetherNetPacket(
                    connection,
                    serialized[index],
                    _server.Properties.CompressionMethod,
                    _server.Properties.CompressionThreshold);
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

    private void SendNetherNetPacket(
        NetworkConnection connection,
        SerializedOutgoing packet,
        string? serverCompressionMethod = null,
        int compressionThreshold = 0) {
        int batchCapacity = packet.Length + 5;
        byte[] batch = ArrayPool<byte>.Shared.Rent(batchCapacity);
        byte[]? frame = ArrayPool<byte>.Shared.Rent(batchCapacity + (batchCapacity >> 12) + (batchCapacity >> 14) + (batchCapacity >> 25) + 16);
        try {
            int offset = 0;
            BinaryWriter writer = new(batch, ref offset);
            writer.WriteVarUInt((uint)packet.Length);
            writer.WriteBytes(packet.Payload.AsSpan(0, packet.Length));

            CompressionMethod compression = packet.Compression ?? GetCompressionMethod(serverCompressionMethod);
            int frameLength = offset;
            if (connection.NetherNetCompression && compression != CompressionMethod.NotPresent) {
                frameLength = PacketCompression.Compress(
                    batch.AsSpan(0, offset),
                    frame,
                    compression == CompressionMethod.Zlib && offset < compressionThreshold
                        ? CompressionMethod.None
                        : compression);
            }
            else {
                batch.AsSpan(0, offset).CopyTo(frame);
            }

            if (!connection.SendOwned(frame, frameLength, packet.Unreliable, packet.Immediate)) {
                connection.SendPacket(frame.AsSpan(0, frameLength), packet.Unreliable, packet.Immediate);
            }
            else {
                frame = null;
            }
            Interlocked.Add(ref _sentBytes, frameLength);
            Interlocked.Increment(ref _sentPackets);
            Interlocked.Increment(ref _sentFrames);
        }
        finally {
            ArrayPool<byte>.Shared.Return(batch);
            if (frame is not null) {
                ArrayPool<byte>.Shared.Return(frame);
            }
        }
    }

    private readonly record struct OutgoingPacket(
        Packet? Packet,
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
        bool Immediate,
        bool Unreliable);

    private readonly record struct IncomingPacket(
        NetworkConnection Connection,
        Packet Packet,
        int Id,
        long QueuedTimestamp,
        ReadOnlyMemory<byte> PacketBuffer);

    private abstract class PacketListener {
        public abstract void Invoke(
            NetworkConnection connection,
            Packet packet
        );

        public abstract bool Matches(Delegate listener);
    }

    private sealed class PacketListener<TPacket> : PacketListener
        where TPacket : Packet {

        private readonly Action<NetworkConnection, TPacket> _listener;

        public PacketListener(
            Action<NetworkConnection, TPacket> listener
        ) {
            _listener = listener;
        }

        public override void Invoke(
            NetworkConnection connection,
            Packet packet
        ) {
            if (packet is not TPacket typedPacket) {
                throw new InvalidOperationException(
                    $"Expected {typeof(TPacket).FullName}, " +
                    $"received {packet.GetType().FullName}."
                );
            }

            _listener(connection, typedPacket);
        }

        public override bool Matches(Delegate listener) {
            return listener is Action<NetworkConnection, TPacket> typedListener &&
                   _listener == typedListener;
        }
    }

    private static void SerializeOutgoingPacket(
        Packet packet,
        BinaryWriter writer) {
        int packetId = _generatedPacketIds.GetOrAdd(
            packet.GetType(),
            static packetType => {
                PacketIdAttribute? attribute = packetType.GetCustomAttributes(typeof(PacketIdAttribute), false)
                    .Cast<PacketIdAttribute>()
                    .SingleOrDefault();

                if (attribute is null) {
                    throw new InvalidOperationException(
                        $"Packet type {packetType.FullName} does not have a PacketId attribute.");
                }

                return attribute.Id;
            });

        writer.WriteVarUInt(checked((uint)packetId));
        packet.Serialize(ref writer);
    }

    private static bool IsUnreliable(Packet? packet) {
        return packet is MoveActorAbsolutePacket or MoveActorDeltaPacket or SetActorMotionPacket;
    }

    private static bool TryDeserializePacket(
        ReadOnlySpan<byte> packetBuffer,
        out Packet? packet,
        out int packetId) {
        int offset = 0;
        BinaryReader reader = new(packetBuffer, ref offset);

        uint header = reader.ReadVarUInt();
        int id = checked((int)(header & 0x3FF));
        packetId = id;

        if (!PacketPool.TryGetPacketType(id, out Type? packetType)) {
            packet = null;
            return false;
        }

        packet = (Packet)Activator.CreateInstance(packetType!)!;

        packet.Deserialize(ref reader);

        return true;
    }

    private static CompressionMethod GetCompressionMethod(string? value) {
        if (value is not null && value.Equals("snappy", StringComparison.OrdinalIgnoreCase)) {
            return CompressionMethod.Snappy;
        }

        return CompressionMethod.Zlib;
    }

}
