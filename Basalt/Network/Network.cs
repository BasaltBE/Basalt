using System.Buffers;
using System.Numerics;
using Basalt.Binary;
using Basalt.Core;
using Basalt.Network.Handlers;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.RakNet;
using Basalt.RakNet.Packets.Enums;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace Basalt.Network;

public sealed class NetworkHandler
{
    private const int MaxPacketBatchSize = 1024 * 1024 * 8;
    private const int MaxPacketSize = 1024 * 1024 * 4;

    private readonly Server _server;

    public NetworkHandler(Server server)
    {
        _server = server;
    }

    public void HandleDisconnected(NetworkConnection connection)
    {
        if (!_server.Players.Remove(connection, out Player? player))
        {
            Logger.Warn("Disconnect received for unknown connection.");
            return;
        }

        (player.Dimension?.World?.Provider ?? _server.GetWorld().Provider).SavePlayerData(player.Xuid, player.WriteToNbt());

        if (player.IsAlive && player.Dimension is not null)
        {
            player.Despawn(new Basalt.Entity.Traits.Types.EntityDespawnOptions(Disconnected: true));
        }

        Logger.Info($"Player {player.Username} disconnected.");
    }

    public void HandlePacket(NetworkConnection connection, ReadOnlyMemory<byte> payload)
    {
        ReadOnlySpan<byte> packetData = payload.Span;
        byte[]? decompressedBuffer = null;

        try
        {
            decompressedBuffer = ArrayPool<byte>.Shared.Rent(MaxPacketSize);
            int decompressedLength = Protocol.Io.Packet.Unframe(packetData, decompressedBuffer, out _);
            if (decompressedLength == 0) return;

            ReadOnlySpan<byte> frame = decompressedBuffer.AsSpan(0, decompressedLength);

            int offset = 0;
            BinaryReader frameReader = new(frame, ref offset);

            while (frameReader.Remaining > 0)
            {
                int packetLength = checked((int)frameReader.ReadVarUInt());
                if (packetLength <= 0 || packetLength > frameReader.Remaining) break;

                ReadOnlySpan<byte> packetBuffer = frameReader.ReadBytes(packetLength);
                if (packetBuffer.Length == 0) continue;


                try
                {
                    int offset2 = 0;
                    BinaryReader packetReader = new(packetBuffer, ref offset2);
                    uint header = packetReader.ReadVarUInt();
                    PacketId packetId = (PacketId)(header & 0x3FF);

                    HandleGamePacket(connection, packetId, packetBuffer);
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"Packet decode/handle error ({packetBuffer.Length} bytes): {exception}");
                }
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"Network error: {exception}");
        }
        finally
        {
            if (decompressedBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(decompressedBuffer);
            }
        }
    }

    private void HandleGamePacket(NetworkConnection connection, PacketId packetId, ReadOnlySpan<byte> packetBuffer)
    {
        // Logger.Debug($"Received packet {packetId}");
        switch (packetId)
        {
            case PacketId.Login:
                Login.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.RequestNetworkSettings:
                RequestNetworkSettings.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.ResourcePackClientResponse:
                ResourcePackClientResponse.Handle(_server, connection, packetBuffer);
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

            case PacketId.PlayerAction:
                PlayerAction.Handle(_server, connection, packetBuffer);
                break;

            case PacketId.InventoryContent:
                InventoryContent.Handle(_server, connection, packetBuffer);
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
        }
    }

    public void SendPacket(NetworkConnection connection, DataPacket packet, CompressionMethod? compression = null)
    {
        SendPackets(connection, [packet], compression);
    }

    public void SendSerializedPacket(
        NetworkConnection connection,
        PacketId packetId,
        ReadOnlySpan<byte> packetPayload,
        CompressionMethod? compression = null)
    {
        using BinaryStream packetBufferStream = BinaryStream.Rent(packetPayload.Length + 16);
        using BinaryStream frameBufferStream = BinaryStream.Rent(packetPayload.Length + 32);

        BinaryWriter packetWriter = packetBufferStream;
        packetWriter.WriteVarInt((int)packetId);
        packetWriter.WriteBytes(packetPayload);

        ReadOnlySpan<byte> packetData = packetWriter.GetProcessedBytes();

        BinaryWriter frameWriter = frameBufferStream;
        frameWriter.WriteVarInt(packetData.Length);
        frameWriter.WriteBytes(packetData);

        SendFrame(connection, frameWriter.GetProcessedBytes(), compression);
    }

    public void SendPackets(NetworkConnection connection, IEnumerable<DataPacket> packets, CompressionMethod? compression = null)
    {
        using BinaryStream packetBufferStream = BinaryStream.Rent(MaxPacketSize);
        using BinaryStream frameBufferStream = BinaryStream.Rent(MaxPacketBatchSize);
        BinaryWriter frameWriter = frameBufferStream;

        foreach (DataPacket packet in packets)
        {
            packetBufferStream.Offset = 0;
            BinaryWriter packetWriter = packetBufferStream;
            Protocol.Io.Packet.Serialize(packet, packetWriter);

            ReadOnlySpan<byte> packetData = packetWriter.GetProcessedBytes();
            frameWriter.WriteVarInt(packetData.Length);
            frameWriter.WriteBytes(packetData);
        }

        SendFrame(connection, frameWriter.GetProcessedBytes(), compression);
    }

    private void SendFrame(NetworkConnection connection, ReadOnlySpan<byte> frame, CompressionMethod? compression)
    {
        CompressionMethod method = compression ?? _server.Options.CompressionMethod;
        if (method != CompressionMethod.None && method != CompressionMethod.NotPresent && frame.Length < _server.Options.CompressionThreshold)
        {
            method = CompressionMethod.None;
        }

        if (method == CompressionMethod.Snappy)
        {
            throw new NotSupportedException("Snappy compression is not supported.");
        }

        int reserve = method == CompressionMethod.Zlib ? 1024 * 1024 : 0;
        int headerSize = method == CompressionMethod.NotPresent ? 1 : 2;
        byte[] compressedBuffer = ArrayPool<byte>.Shared.Rent(frame.Length + reserve + headerSize);

        try
        {
            compressedBuffer[0] = 0xFE;

            int payloadOffset = 1;
            if (method != CompressionMethod.NotPresent)
            {
                compressedBuffer[1] = (byte)method;
                payloadOffset = 2;
            }

            int payloadLength;
            if (method == CompressionMethod.Zlib)
            {
                payloadLength = Protocol.Io.Packet.Compress(frame, compressedBuffer.AsSpan(payloadOffset));
            }
            else
            {
                frame.CopyTo(compressedBuffer.AsSpan(payloadOffset));
                payloadLength = frame.Length;
            }

            connection.SendPacket(compressedBuffer.AsSpan(0, payloadOffset + payloadLength), Reliability.ReliableOrdered);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(compressedBuffer);
        }
    }

}
