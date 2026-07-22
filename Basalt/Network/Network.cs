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
    private readonly ConcurrentQueue<(NetworkConnection Connection, byte[] Payload)> _incomingPackets = new();
    private readonly ConcurrentQueue<NetworkConnection> _disconnections = new();

    public NetworkHandler(Server server) {
        _server = server;
    }

    /// <summary>
    /// Enqueues a raw packet received from the network thread for processing on the main thread.
    /// </summary>
    public void EnqueuePacket(NetworkConnection connection, ReadOnlyMemory<byte> payload) {
        _incomingPackets.Enqueue((connection, payload.ToArray()));
    }

    /// <summary>
    /// Enqueues a disconnection event for processing on the main thread.
    /// </summary>
    public void EnqueueDisconnection(NetworkConnection connection) {
        _disconnections.Enqueue(connection);
    }

    /// <summary>
    /// Processes all queued packets and disconnections on the main thread.
    /// </summary>
    public void ProcessIncoming() {
        using var __zone = Profiler.BeginZone("Network.ProcessIncoming");

        while (_disconnections.TryDequeue(out NetworkConnection? connection)) {
            try {
                HandleDisconnected(connection);
            }
            catch (Exception exception) {
                Logger.Warn($"Unhandled disconnect error: {exception}");
            }
        }

        while (_incomingPackets.TryDequeue(out var packet)) {
            HandlePacket(packet.Connection, packet.Payload);
        }
    }

    public void HandleDisconnected(NetworkConnection connection) {
        if (!_server.Players.TryRemove(connection, out Player.Player? player)) {
            return;
        }

        Entities.Traits.Types.EntityDespawnOptions options = new(Disconnected: true);
        _server.Emit(new PlayerLeaveSignal(player, options));

        _server.GetWorld().Provider.SavePlayerData(player.Xuid, player.Write());


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

    private void HandlePacket(NetworkConnection connection, ReadOnlyMemory<byte> payload) {
        using var __zone = Profiler.BeginZone("Network.HandlePacket");
        ReadOnlySpan<byte> packetData = payload.Span;
        byte[]? decompressedBuffer = null;

        try {
            decompressedBuffer = ArrayPool<byte>.Shared.Rent(MaxPacketSize);
            int decompressedLength = Protocol.Io.Packet.Unframe(packetData, decompressedBuffer, out _);
            if (decompressedLength == 0) return;

            ReadOnlySpan<byte> frame = decompressedBuffer.AsSpan(0, decompressedLength);

            int offset = 0;
            BinaryReader frameReader = new(frame, ref offset);

            while (frameReader.Remaining > 0) {
                int packetLength = checked((int)frameReader.ReadVarUInt());
                if (packetLength <= 0 || packetLength > frameReader.Remaining) break;

                ReadOnlySpan<byte> packetBuffer = frameReader.ReadBytes(packetLength);
                if (packetBuffer.Length == 0) continue;


                try {
                    int offset2 = 0;
                    BinaryReader packetReader = new(packetBuffer, ref offset2);
                    uint header = packetReader.ReadVarUInt();
                    PacketId packetId = (PacketId)(header & 0x3FF);

                    HandleGamePacket(connection, packetId, packetBuffer);
                }
                catch (Exception exception) {
                    Console.WriteLine($"Packet decode/handle error ({packetBuffer.Length} bytes): {exception}");
                }
            }
        }
        catch (Exception exception) {
            Console.WriteLine($"Network error: {exception}");
        }
        finally {
            if (decompressedBuffer is not null) {
                ArrayPool<byte>.Shared.Return(decompressedBuffer);
            }
        }
    }

    private void HandleGamePacket(NetworkConnection connection, PacketId packetId, ReadOnlySpan<byte> packetBuffer) {
        _server.Players.TryGetValue(connection, out Player.Player? packetPlayer);
        PacketReceiveSignal receiveSignal = new(connection, packetPlayer, packetId, packetBuffer.ToArray());
        _server.Emit(receiveSignal);
        if (receiveSignal.Cancelled) {
            return;
        }

        switch (packetId) {
            case PacketId.Login:
                Login.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.RequestNetworkSettings:
                RequestNetworkSettings.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.ResourcePackClientResponse:
                ResourcePackClientResponse.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.ResourcePackChunkRequest:
                ResourcePackChunkRequest.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.RequestChunkRadius:
                RequestChunkRadius.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.SetLocalPlayerAsInitialized:
                SetLocalPlayerAsInitialized.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.PlayerAuthInput:
                PlayerAuthInput.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.Interact:
                Interact.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.ContainerClose:
                ContainerClose.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.InventoryTransaction:
                InventoryTransaction.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.MobEquipment:
                MobEquipment.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.PlayerAction:
                PlayerAction.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.Respawn:
                Respawn.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.ItemStackRequest:
                ItemStackRequest.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.ClientCacheStatus:
                ClientCacheStatus.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.CommandRequest:
                CommandRequest.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.Text:
                Text.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.ModalFormResponse:
                ModalFormResponse.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.ServerboundDataStore:
                ServerboundDataStore.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.ServerboundDataDrivenScreenClosed:
                break;

            case PacketId.ServerboundSettingsCommand:
                break;
        }
    }

    public void SendPacket(NetworkConnection connection, DataPacket packet, CompressionMethod? compression = null, bool immediate = false) {
        _server.Players.TryGetValue(connection, out Player.Player? player);
        PacketSendSignal sendSignal = new(connection, player, packet);
        _server.Emit(sendSignal);
        if (sendSignal.Cancelled) {
            return;
        }

        SendPackets(connection, [packet], compression, immediate);
    }

    public void SendSerializedPacket(
        NetworkConnection connection,
        PacketId packetId,
        ReadOnlySpan<byte> packetPayload,
        CompressionMethod? compression = null,
        bool immediate = false) {
        using BinaryStream packetBufferStream = BinaryStream.Rent(packetPayload.Length + 16);
        using BinaryStream frameBufferStream = BinaryStream.Rent(packetPayload.Length + 32);

        BinaryWriter packetWriter = packetBufferStream;
        packetWriter.WriteVarInt((int)packetId);
        packetWriter.WriteBytes(packetPayload);

        ReadOnlySpan<byte> packetData = packetWriter.GetProcessedBytes();

        BinaryWriter frameWriter = frameBufferStream;
        frameWriter.WriteVarInt(packetData.Length);
        frameWriter.WriteBytes(packetData);

        SendFrame(connection, frameWriter.GetProcessedBytes(), compression, immediate);
    }

    public void SendPackets(NetworkConnection connection, IEnumerable<DataPacket> packets, CompressionMethod? compression = null, bool immediate = false) {
        using BinaryStream packetBufferStream = BinaryStream.Rent(MaxPacketSize);
        using BinaryStream frameBufferStream = BinaryStream.Rent(MaxPacketBatchSize);
        BinaryWriter frameWriter = frameBufferStream;

        foreach (DataPacket packet in packets) {
            // if (packet.GetType().Name != "LevelChunkPacket")
            // Logger.Info("Sending packet {0}", packet.GetType().Name);

            packetBufferStream.Offset = 0;
            BinaryWriter packetWriter = packetBufferStream;
            Protocol.Io.Packet.Serialize(packet, packetWriter);

            ReadOnlySpan<byte> packetData = packetWriter.GetProcessedBytes();
            frameWriter.WriteVarInt(packetData.Length);
            frameWriter.WriteBytes(packetData);
        }

        SendFrame(connection, frameWriter.GetProcessedBytes(), compression, immediate);
    }

    private void SendFrame(NetworkConnection connection, ReadOnlySpan<byte> frame, CompressionMethod? compression, bool immediate = false) {
        CompressionMethod method = compression ?? GetCompressionMethod(_server.Properties.CompressionMethod);
        if (method != CompressionMethod.None && method != CompressionMethod.NotPresent && frame.Length < _server.Properties.CompressionThreshold) {
            method = CompressionMethod.None;
        }

        if (method == CompressionMethod.Snappy) {
            throw new NotSupportedException("Snappy compression is not supported.");
        }

        int reserve = method == CompressionMethod.Zlib ? 1024 * 1024 : 0;
        int headerSize = method == CompressionMethod.NotPresent ? 1 : 2;
        byte[] compressedBuffer = ArrayPool<byte>.Shared.Rent(frame.Length + reserve + headerSize);

        try {
            compressedBuffer[0] = 0xFE;

            int payloadOffset = 1;
            if (method != CompressionMethod.NotPresent) {
                compressedBuffer[1] = (byte)method;
                payloadOffset = 2;
            }

            int payloadLength;
            if (method == CompressionMethod.Zlib) {
                payloadLength = Protocol.Io.Packet.Compress(frame, compressedBuffer.AsSpan(payloadOffset));
            }
            else {
                frame.CopyTo(compressedBuffer.AsSpan(payloadOffset));
                payloadLength = frame.Length;
            }

            connection.SendPacket(compressedBuffer.AsSpan(0, payloadOffset + payloadLength), Reliability.ReliableOrdered, immediate);
        }
        finally {
            ArrayPool<byte>.Shared.Return(compressedBuffer);
        }
    }

    private static CompressionMethod GetCompressionMethod(string? value) {
        if (value is not null && value.Equals("snappy", StringComparison.OrdinalIgnoreCase)) {
            return CompressionMethod.Snappy;
        }

        return CompressionMethod.Zlib;
    }

}










