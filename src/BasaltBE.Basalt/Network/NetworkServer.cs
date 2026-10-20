using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Basalt.Binary;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Basalt.Events.Players;
using BasaltBE.Basalt.Network.Events;
using BasaltBE.Basalt.Network.Handlers;
using BasaltBE.Basalt.Network.ResourcePacks;
using BasaltBE.Basalt.Network.Types;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Logging;
using BasaltBE.NetherNet;
using BasaltBE.NetherNet.Enums;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.Network;

public sealed class NetworkServer : IDisposable {
    private const int MaximumBatchLength = 4 * 1024 * 1024;
    internal const ushort CompressionThreshold = 512;
    private static readonly ConcurrentDictionary<Type, uint> packetIds = [];
    private readonly NethernetServer nethernet;
    internal readonly LoginHandler Login;
    internal readonly Server Server;
    internal readonly ResourcePackManager ResourcePacks;
    internal readonly ServerAdvertisement Advertisement;
    public readonly SerializedPacketPalette SerializedPackets;
    // For now players live here, they will be moved to per world
    private readonly ConcurrentDictionary<NethernetPeer, Player> players = [];
    private readonly ConcurrentQueue<Player> removedPlayers = new();
    private readonly Lock rosterSync = new();
    private readonly ConcurrentDictionary<NethernetPeer, byte> compressionPeers = [];
    private int disposed;

    private readonly ConcurrentDictionary<NethernetPeer, InboundConnection> inbound = [];

    private sealed class InboundConnection {
        internal int Messages;
        internal long Bytes;
        internal int Closed;
    }

    public readonly EventEmitter Events = new();

    internal NetworkServer(
        Server server,
        NethernetServerOptions options,
        IServerIdentity? identity,
        ResourcePackManager resourcePacks
    ) {
        Server = server;
        nethernet = new NethernetServer(options, identity);
        Advertisement = options.Advertisement;
        ResourcePacks = resourcePacks;
        SerializedPackets = new SerializedPacketPalette(this);
        Login = new LoginHandler(this);
        Events.Subscribe(PacketEvents.RequestNetworkSettings, new RequestNetworkSettingsHandler(this).Handle);
        Events.Subscribe(PacketEvents.Login, Login.Handle);
        Events.Subscribe(PacketEvents.ClientCacheStatus, new ClientCacheStatusHandler(this).Handle);
        Events.Subscribe(PacketEvents.ContainerClose, new ContainerCloseHandler().Handle);
        Events.Subscribe(PacketEvents.Interact, new InteractHandler().Handle);
        Events.Subscribe(PacketEvents.InventoryTransaction, new InventoryTransactionHandler().Handle);
        Events.Subscribe(PacketEvents.MobEquipment, new MobEquipmentHandler().Handle);
        Events.Subscribe(PacketEvents.Animate, new AnimateHandler().Handle);
        Events.Subscribe(PacketEvents.Text, new TextHandler().Handle);
        Events.Subscribe(PacketEvents.CommandRequest, new CommandRequestHandler().Handle);
        Events.Subscribe(PacketEvents.ItemStackRequest, new ItemStackRequestHandler().Handle);
        Events.Subscribe(PacketEvents.PlayerAuthInput, new PlayerAuthInputHandler().Handle);
        Events.Subscribe(PacketEvents.RequestChunkRadius, new RequestChunkRadiusHandler(this).Handle);
        Events.Subscribe(PacketEvents.SetLocalPlayerAsInitialized, new SetLocalPlayerAsInitializedHandler().Handle);
        ResourcePackClientResponseHandler resourcePackResponse = new(this);
        Events.Subscribe(PacketEvents.ResourcePackClientResponse, resourcePackResponse.Handle);
        Events.Subscribe(PacketEvents.ResourcePackChunkRequest, new ResourcePackChunkRequestHandler(this).Handle);
    }

    public ulong NetworkId => nethernet.NetworkId;

    public void Start() {
        using ProfilerZone zone = Profiler.BeginZone("Network.Start");
        Login.Start();
        nethernet.Start(OnMessage);
    }

    public void SendPacket(
        Player player,
        DataPacket packet,
        bool compress = true,
        PacketCompressionAlgorithm method = PacketCompressionAlgorithm.ZLib
    ) {
        ArgumentNullException.ThrowIfNull(player);
        if (!players.TryGetValue(player.Peer, out Player? current) || !ReferenceEquals(current, player))
            throw new PeerDisconnectedException(player.Peer);
        SendPacket(player.Peer, player, packet, compress, method);
    }

    internal void SendSerializedPacket(
        Player player,
        DataPacket packet,
        ReadOnlyMemory<byte> serializedPayload,
        bool compress = true,
        PacketCompressionAlgorithm method = PacketCompressionAlgorithm.ZLib
    ) {
        ArgumentNullException.ThrowIfNull(player);
        if (!players.TryGetValue(player.Peer, out Player? current) || !ReferenceEquals(current, player))
            throw new PeerDisconnectedException(player.Peer);
        SendPacket(player.Peer, player, packet, compress, method, serializedPayload);
    }

    internal bool CompressionNegotiated(NethernetPeer peer) => compressionPeers.ContainsKey(peer);

    internal void SetCompression(NethernetPeer peer) => compressionPeers.TryAdd(peer, 0);

    internal byte[] PreparePacket(DataPacket packet) {
        using BinaryStream body = BinaryStream.Rent(MaximumBatchLength);
        BinaryWriter writer = body.GetWriter();
        writer.WriteVarUInt(GetPacketId(packet));
        packet.Serialize(ref writer);
        return PrepareBatch(body.Buffer[..body.Offset]);
    }

    private static byte[] PrepareBatch(ReadOnlyMemory<byte> packet) {
        if (packet.Length > MaximumBatchLength - 5)
            throw new InvalidDataException("Bedrock packet batch exceeds the configured limit.");

        using BinaryStream framed = BinaryStream.Rent(packet.Length + 5);
        int batchOffset = 0;
        BinaryWriter batchWriter = new(framed.Buffer.Span, ref batchOffset);
        batchWriter.WriteVarUInt((uint)packet.Length);
        packet.Span.CopyTo(framed.Buffer.Span[batchOffset..]);
        int batchLength = batchOffset + packet.Length;
        ReadOnlyMemory<byte> payload = framed.Buffer[..batchLength];
        if (payload.Length >= CompressionThreshold) {
            using PooledBufferStream compressedBatch = new(payload.Length + 1);
            compressedBatch.WriteByte((byte)PacketCompressionAlgorithm.ZLib);
            using (DeflateStream deflater = new(compressedBatch, CompressionLevel.Fastest, true))
                deflater.Write(payload.Span);
            if (compressedBatch.Length - 1 < payload.Length)
                return compressedBatch.WrittenMemory.ToArray();
        }

        byte[] markedBatch = new byte[payload.Length + 1];
        markedBatch[0] = unchecked((byte)PacketCompressionAlgorithm.None);
        payload.Span.CopyTo(markedBatch.AsSpan(1));
        return markedBatch;
    }

    internal void SendPreparedPacket(Player player, ReadOnlyMemory<byte> payload) {
        ArgumentNullException.ThrowIfNull(player);
        if (!players.TryGetValue(player.Peer, out Player? current) || !ReferenceEquals(current, player))
            throw new PeerDisconnectedException(player.Peer);

        using Lock.Scope sync = player.Sync.EnterScope();
        if (player.Disposed)
            throw new ObjectDisposedException(nameof(Player));
        nethernet.Send(player.Peer, payload, MessageReliability.Reliable);
    }

    internal bool PeerConnected(NethernetPeer peer) =>
        inbound.TryGetValue(peer, out InboundConnection? connection) && Volatile.Read(ref connection.Closed) == 0;

    internal void Disconnect(NethernetPeer peer) => nethernet.Disconnect(peer);

    internal void SendPacket(
        NethernetPeer peer,
        DataPacket packet,
        bool compress = true,
        PacketCompressionAlgorithm method = PacketCompressionAlgorithm.ZLib
    ) => SendPacket(peer, null, packet, compress, method);

    internal void AddPlayer(Player player) {
        using ProfilerZone zone = Profiler.BeginZone("Network.AddPlayer");
        long rosterLength = player.Skin.Length + Encoding.UTF8.GetByteCount(player.Username) +
            Encoding.UTF8.GetByteCount(player.Xuid) + 64L;
        foreach (Player existing in players.Values)
            rosterLength += existing.Skin.Length + Encoding.UTF8.GetByteCount(existing.Username) +
                Encoding.UTF8.GetByteCount(existing.Xuid) + 64L;
        if (players.Count >= 1000 || rosterLength > MaximumBatchLength - 16)
            throw new InvalidDataException("The player list exceeds the supported network payload size.");
        if (!players.TryAdd(player.Peer, player))
            throw new InvalidDataException("Peer already has a Bedrock player.");

        try {
            player.World.AddPlayer(player);
        }
        catch {
            players.TryRemove(player.Peer, out _);
            throw;
        }
    }

    internal void RemovePlayer(NethernetPeer peer) {
        using ProfilerZone zone = Profiler.BeginZone("Network.RemovePlayer");
        if (!players.TryGetValue(peer, out Player? player))
            return;

        using Lock.Scope tickSync = player.TickSync.EnterScope();
        if (!players.TryRemove(peer, out Player? removed) || !ReferenceEquals(player, removed))
            return;

        player.OnDespawn();
        player.World.RemovePlayer(player);
        lock (rosterSync) {
            if (player.Listed) {
                player.Listed = false;
                if (!Server.Started)
                    removedPlayers.Enqueue(player);
                else {
                    foreach (var world in Server.Worlds) {
                        foreach (Dimension dimension in world.Dimensions.Values)
                            dimension.RemovedPlayers.Enqueue(player);
                    }
                }
            }
        }
        player.Dispose();
        PlayerDisconnectEvent disconnected = new(player);
        Server.Events.Emit(disconnected);
    }

    internal void Tick(Dimension? dimension = null) {
        ReadOnlySpan<Player> currentPlayers = dimension is null ? players.Values.ToArray() : dimension.GetPlayers();
        ConcurrentQueue<Player> removals = dimension?.RemovedPlayers ?? removedPlayers;
        while (removals.TryDequeue(out Player? removed)) {
            PlayerListPacket removal = new() {
                Entries = [new PlayerListEntry
                {
                    Action = PlayerListAction.Remove,
                    RemoveEntry = new PlayerListRemoveEntry { Uuid = removed.ListEntry.AddEntry!.Uuid }
                }]
            };
            foreach (Player recipient in currentPlayers) {
                using Lock.Scope tickSync = recipient.TickSync.EnterScope();
                if (!recipient.Listed || recipient.Disposed)
                    continue;
                try {
                    recipient.GetTrait<Entities.Traits.PlayerChunkRenderingTrait>()?.RemoveEntity(removed);
                    SendPacket(recipient, removal);
                }
                catch (Exception error) when (error is PeerDisconnectedException or ObjectDisposedException) {
                    // Remaining ppl still need the removal.
                }
            }
        }
        foreach (Player player in currentPlayers) {
            using Lock.Scope tickSync = player.TickSync.EnterScope();
            if (player.Spawned && !player.Disposed && !player.Listed) {
                try {
                    UpdatePlayerList(player);
                }
                catch (Exception error) when (error is PeerDisconnectedException or ObjectDisposedException) {
                    RemovePlayer(player.Peer);
                }
            }
        }
    }

    internal void UpdatePlayerList(Player player) {
        using Lock.Scope rosterLock = rosterSync.EnterScope();
        if (player.Listed || player.Disposed || !player.Spawned)
            return;
        Player[] existing = players.Values.Where(other => other.Listed && !other.Disposed &&
            !ReferenceEquals(other, player)).ToArray();
        PlayerListPacket roster = new() { Entries = new PlayerListEntry[existing.Length] };

        for (int index = 0; index < existing.Length; index++)
            roster.Entries[index] = existing[index].ListEntry;

        SendPacket(player, roster);
        player.Listed = true;

        PlayerListPacket addition = new() { Entries = [player.ListEntry] };
        using BinaryStream stream = BinaryStream.Rent(player.Skin.Length + Encoding.UTF8.GetByteCount(player.Username) +
            Encoding.UTF8.GetByteCount(player.Xuid) + 64);
        BinaryWriter writer = stream.GetWriter();
        addition.Serialize(ref writer);

        if (existing.Length != 0)
            SendPacket(existing[0].Peer, existing[0], addition, true, PacketCompressionAlgorithm.ZLib,
                stream.Buffer[..stream.Offset], existing);
    }

    public void Dispose() {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;
        using ProfilerZone zone = Profiler.BeginZone("Network.Dispose");
        nethernet.Dispose();
        Login.Dispose();
        foreach (NethernetPeer peer in players.Keys)
            RemovePlayer(peer);
        removedPlayers.Clear();
        compressionPeers.Clear();
        inbound.Clear();
    }

    internal void OnMessage(NethernetMessage message) {
        if (message.Connected) {
            if (inbound.Count >= 4096)
                nethernet.Disconnect(message.Peer);
            else
                inbound.TryAdd(message.Peer, new InboundConnection());
            return;
        }
        if (!Server.Started) {
            ProcessMessage(message);
            return;
        }
        if (!inbound.TryGetValue(message.Peer, out InboundConnection? connection) ||
            Volatile.Read(ref connection.Closed) != 0)
            return;
        var world = players.TryGetValue(message.Peer, out Player? player) ? player.World : Server.GetWorld();
        if (message.Disconnected) {
            Interlocked.Exchange(ref connection.Closed, 1);
            world.Enqueue(message);
            return;
        }
        int length = message.Payload.Length;
        int messages = Interlocked.Increment(ref connection.Messages);
        long bytes = Interlocked.Add(ref connection.Bytes, length);
        if (messages <= 256 && bytes <= MaximumBatchLength * 2L && world.Enqueue(message))
            return;
        Interlocked.Decrement(ref connection.Messages);
        Interlocked.Add(ref connection.Bytes, -length);
        if (Interlocked.Exchange(ref connection.Closed, 1) == 0) {
            Logger.Scoped("Network").Warn($"Disconnecting {message.Peer.ConnectionId}: inbound queue limit reached.");
            world.Enqueue(new NethernetMessage(message.Peer, message.Reliability, default, Disconnected: true));
            nethernet.Disconnect(message.Peer);
        }
    }

    internal void ReleaseInbound(NethernetMessage message) {
        if (inbound.TryGetValue(message.Peer, out InboundConnection? connection)) {
            Interlocked.Decrement(ref connection.Messages);
            Interlocked.Add(ref connection.Bytes, -message.Payload.Length);
        }
    }

    internal void ProcessMessage(NethernetMessage message) {
        using ProfilerZone messageZone = Profiler.BeginZone("Network.OnMessage");
        if (message.Connected)
            return;

        if (message.Disconnected) {
            Login.Disconnected(message.Peer);
            if (players.TryGetValue(message.Peer, out Player? disconnectedPlayer))
                Logger.Scoped("Network").Info($"Player {disconnectedPlayer.Username} Disconnected.");
            RemovePlayer(message.Peer);
            compressionPeers.TryRemove(message.Peer, out _);
            inbound.TryRemove(message.Peer, out _);
            return;
        }

        Player? player = players.GetValueOrDefault(message.Peer);
        byte[]? decompressed = null;
        uint? currentPacketId = null;
        ReadOnlyMemory<byte> currentPacketPayload = default;
        bool compressionNegotiated = player is not null || CompressionNegotiated(message.Peer);
        try {
            ReadOnlyMemory<byte> payload = message.Payload;
            ReadOnlyMemory<byte> batch = payload;
            if (compressionNegotiated) {
                if (payload.IsEmpty)
                    throw new InvalidDataException("Bedrock packet batch is empty.");
                byte marker = payload.Span[0];
                if (marker == unchecked((byte)PacketCompressionAlgorithm.None))
                    batch = payload[1..];
                else if (marker == (byte)PacketCompressionAlgorithm.Snappy)
                    throw new NotSupportedException("Bedrock Snappy compression is not supported.");
                else if (marker == (byte)PacketCompressionAlgorithm.ZLib) {
                    using ProfilerZone decompressZone = Profiler.BeginZone("Network.Decompress");
                    batch = Decompress(payload[1..], ref decompressed);
                }
                else {
                    throw new InvalidDataException($"Unsupported Bedrock compression marker 0x{marker:X2}.");
                }
            }

            if (batch.IsEmpty || batch.Length > MaximumBatchLength)
                throw new InvalidDataException("Bedrock packet batch length is invalid.");

            int offset = 0;
            while (offset < batch.Length) {
                using ProfilerZone packetZone = Profiler.BeginZone("Network.ReadPacket");
                BinaryReader reader = new(batch.Span, ref offset);
                uint packetLength = reader.ReadVarUInt();
                if (packetLength == 0 || packetLength > reader.Remaining)
                    throw new InvalidDataException("Bedrock packet length is invalid.");
                int packetEnd = checked(offset + (int)packetLength);
                BinaryReader packetReader = new(batch.Span[..packetEnd], ref offset);
                uint packetId = packetReader.ReadVarUInt();
                currentPacketId = packetId;
                ReadOnlyMemory<byte> packetPayload = batch.Slice(offset, packetEnd - offset);
                currentPacketPayload = packetPayload;
                offset = packetEnd;
                Logger.Scoped("Network").Debug(
                    $"Received Bedrock packet 0x{packetId:X} ({packetLength} bytes) from {message.Peer.ConnectionId}."
                );
                if (player is null && packetId != PacketEvents.Login.PacketId && packetId != PacketEvents.RequestNetworkSettings.PacketId)
                    throw new InvalidDataException("Bedrock packet arrived before Login.");
                bool handled;
                using (Profiler.BeginZone("Network.DispatchPacket"))
                    handled = Events.Emit(packetId, message.Peer, player, packetPayload);
                if (!handled) {
                    Logger.Scoped("Network").Debug($"Ignoring unhandled Bedrock packet 0x{packetId:X} ({packetLength} bytes) from {message.Peer.ConnectionId}.");
                    continue;
                }

                player = players.GetValueOrDefault(message.Peer);
            }
        }
        catch (Exception error) when (
            error is InvalidDataException
                or EndOfStreamException
                or NotSupportedException
                or CryptographicException
                or JsonException
                or InvalidOperationException
                or ArgumentException
                or IOException
                or HttpRequestException
                or TaskCanceledException
        ) {
            ReadOnlySpan<byte> packetPrefix = currentPacketPayload.Span[..Math.Min(24, currentPacketPayload.Length)];
            ReadOnlySpan<byte> payloadPrefix = message.Payload.Span[..Math.Min(8, message.Payload.Length)];
            Logger.Scoped("Network").Error(
                $"Bedrock packet processing failed for {message.Peer.ConnectionId} at packet 0x{currentPacketId:X}: {error.Message} " +
                $"(compression: {(compressionNegotiated ? PacketCompressionAlgorithm.ZLib.ToString() : "not negotiated")}, " +
                $"packet prefix: {Convert.ToHexString(packetPrefix)}, payload prefix: {Convert.ToHexString(payloadPrefix)})."
            );
        }
        finally {
            if (decompressed is not null) {
                CryptographicOperations.ZeroMemory(decompressed);
                ArrayPool<byte>.Shared.Return(decompressed);
            }
        }
    }

    private void SendPacket(
        NethernetPeer peer,
        Player? player,
        DataPacket packet,
        bool compress,
        PacketCompressionAlgorithm method,
        ReadOnlyMemory<byte>? serializedPayload = null,
        Player[]? recipients = null
    ) {
        using ProfilerZone sendZone = Profiler.BeginZone("Network.SendPacket");
        ArgumentNullException.ThrowIfNull(packet);
        uint packetId = GetPacketId(packet);

        using Lock.Scope sync = player is null || recipients is not null ? default : player.Sync.EnterScope();
        if (recipients is null && player?.Disposed == true)
            throw new ObjectDisposedException(nameof(Player));
        using BinaryStream packetStream = BinaryStream.Rent(MaximumBatchLength);
        byte[]? markedBatch = null;
        PooledBufferStream? compressedBatch = null;
        int markedBatchLength = 0;
        try {
            packetStream.Offset = 5;
            using (Profiler.BeginZone("Network.SerializePacket")) {
                BinaryWriter packetWriter = packetStream.GetWriter();
                packetWriter.WriteVarUInt(packetId);
                if (serializedPayload is ReadOnlyMemory<byte> cachedPayload)
                    packetWriter.WriteBytes(cachedPayload.Span);
                else
                    packet.Serialize(ref packetWriter);
            }
            int packetLength = packetStream.Offset - 5;
            if (packetLength > MaximumBatchLength - 5)
                throw new InvalidDataException("Bedrock packet batch exceeds the configured limit.");
            Logger.Scoped("Network").Debug(
                $"Sending Bedrock packet 0x{packetId:X} ({packet.GetType().Name}, {packetLength} bytes) to {peer.ConnectionId}."
            );

            int batchOffset = 0;
            BinaryWriter batchWriter = new(packetStream.Buffer.Span, ref batchOffset);
            batchWriter.WriteVarUInt((uint)packetLength);
            packetStream.Buffer.Span[5..packetStream.Offset].CopyTo(packetStream.Buffer.Span[batchOffset..]);
            int batchLength = batchOffset + packetLength;
            ReadOnlyMemory<byte> payload = packetStream.Buffer[..batchLength];
            bool compressionNegotiated = player is not null || CompressionNegotiated(peer);

            if (compressionNegotiated && compress) {
                using ProfilerZone compressionZone = Profiler.BeginZone("Network.CompressPacket");
                if (method == PacketCompressionAlgorithm.Snappy)
                    throw new NotSupportedException("Bedrock Snappy compression is not supported.");
                if (method == PacketCompressionAlgorithm.ZLib && payload.Length >= CompressionThreshold) {
                    compressedBatch = new PooledBufferStream(payload.Length + 1);
                    compressedBatch.WriteByte((byte)method);
                    using (DeflateStream deflater = new(compressedBatch, CompressionLevel.Fastest, true))
                        deflater.Write(payload.Span);
                    if (compressedBatch.Length - 1 < payload.Length) {
                        payload = compressedBatch.WrittenMemory;
                    }
                    else {
                        compressedBatch.Dispose();
                        compressedBatch = null;
                    }
                }

                if (compressedBatch is null) {
                    markedBatchLength = payload.Length + 1;
                    markedBatch = ArrayPool<byte>.Shared.Rent(markedBatchLength);
                    markedBatch[0] = unchecked((byte)PacketCompressionAlgorithm.None);
                    payload.Span.CopyTo(markedBatch.AsSpan(1));
                    payload = markedBatch.AsMemory(0, markedBatchLength);
                }
            }

            using (Profiler.BeginZone("Network.TransportSend")) {
                if (recipients is null) {
                    nethernet.Send(peer, payload, MessageReliability.Reliable);
                }
                else {
                    foreach (Player recipient in recipients) {
                        using Lock.Scope recipientSync = recipient.Sync.EnterScope();
                        if (recipient.Disposed || !players.TryGetValue(recipient.Peer, out Player? current) ||
                            !ReferenceEquals(current, recipient))
                            continue;
                        try {
                            nethernet.Send(recipient.Peer, payload, MessageReliability.Reliable);
                        }
                        catch (PeerDisconnectedException) {
                        }
                    }
                }
            }
        }
        finally {
            compressedBatch?.Dispose();
            if (markedBatch is not null) {
                CryptographicOperations.ZeroMemory(markedBatch.AsSpan(0, markedBatchLength));
                ArrayPool<byte>.Shared.Return(markedBatch);
            }
            CryptographicOperations.ZeroMemory(packetStream.Buffer.Span[..packetStream.Offset]);
        }
    }

    private static ReadOnlyMemory<byte> Decompress(ReadOnlyMemory<byte> compressed, ref byte[]? decompressed) {
        using ProfilerZone decompressZone = Profiler.BeginZone("Network.DecompressData");
        MemoryStream compressedStream = MemoryMarshal.TryGetArray(compressed, out ArraySegment<byte> segment)
            ? new MemoryStream(segment.Array!, segment.Offset, segment.Count, false)
            : new MemoryStream(compressed.ToArray(), false);
        using (compressedStream)
        using (DeflateStream decompressor = new(compressedStream, CompressionMode.Decompress)) {
            decompressed = ArrayPool<byte>.Shared.Rent(Math.Max(256, Math.Min(compressed.Length, MaximumBatchLength)));
            int length = 0;
            while (length < MaximumBatchLength) {
                if (length == decompressed.Length) {
                    int capacity = decompressed.Length >= MaximumBatchLength / 2
                        ? MaximumBatchLength
                        : decompressed.Length * 2;
                    byte[] expanded = ArrayPool<byte>.Shared.Rent(capacity);
                    decompressed.AsSpan(0, length).CopyTo(expanded);
                    CryptographicOperations.ZeroMemory(decompressed);
                    ArrayPool<byte>.Shared.Return(decompressed);
                    decompressed = expanded;
                }

                int read = decompressor.Read(decompressed.AsSpan(
                    length,
                    Math.Min(decompressed.Length - length, MaximumBatchLength - length)
                ));
                if (read == 0) break;
                length += read;
            }

            if (length == 0)
                throw new InvalidDataException("Decompressed Bedrock packet batch is empty.");
            if (length == MaximumBatchLength && decompressor.ReadByte() != -1)
                throw new InvalidDataException("Decompressed Bedrock packet batch exceeds the configured limit.");
            return decompressed.AsMemory(0, length);
        }
    }

    private static uint GetPacketId(DataPacket packet) => packetIds.GetOrAdd(packet.GetType(), static type => {
        PacketIdAttribute? attribute = type.GetCustomAttribute<PacketIdAttribute>();
        if (attribute is null)
            throw new InvalidOperationException($"{type.Name} has no packet ID attribute.");
        return checked((uint)attribute.Id);
    });
}
