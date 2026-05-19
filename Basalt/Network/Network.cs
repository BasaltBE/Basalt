using System.Buffers;
using System.IO.Compression;
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

        try
        {
            _server.World.Provider.SavePlayerData(player.Xuid, player.WriteToNbt());
        }
        catch (Exception exception)
        {
            Logger.Warn($"Failed saving player data for {player.Username}: {exception.Message}");
        }

        try
        {
            if (player.IsAlive && player.Dimension is not null)
            {
                player.Despawn(new Basalt.Entity.Traits.Types.EntityDespawnOptions(Disconnected: true));
            }
        }
        catch (Exception exception)
        {
            Logger.Warn($"Failed despawning {player.Username} during disconnect: {exception.Message}");
        }

        Logger.Info($"Player {player.Username} disconnected.");
    }

    public void HandlePacket(NetworkConnection connection, ReadOnlyMemory<byte> payload)
    {
        ReadOnlySpan<byte> packetData = payload.Span;

        if (packetData.Length == 0 || packetData[0] != 0xFE)
        {
            return;
        }

        packetData = packetData[1..];
        if (packetData.Length == 0)
        {
            return;
        }

        CompressionMethod compression = (CompressionMethod)packetData[0];

        if (compression is CompressionMethod.Zlib or CompressionMethod.Snappy or CompressionMethod.None)
        {
            packetData = packetData[1..];
        }
        else
        {
            compression = CompressionMethod.NotPresent;
        }

        byte[]? decompressedBuffer = null;

        try
        {
            ReadOnlySpan<byte> frame;

            if (compression == CompressionMethod.Zlib)
            {
                decompressedBuffer = ArrayPool<byte>.Shared.Rent(MaxPacketSize);
                int decompressedLength = Decompress(packetData, decompressedBuffer);
                frame = decompressedBuffer.AsSpan(0, decompressedLength);
            }
            else
            {
                frame = packetData;
            }

            int offset = 0;
            BinaryReader frameReader = new(frame, ref offset);

            while (frameReader.Remaining > 0)
            {
                int packetLength;

                packetLength = checked((int)frameReader.ReadVarUInt());

                if (packetLength <= 0 || packetLength > frameReader.Remaining)
                {
                    break;
                }

                ReadOnlySpan<byte> packetBuffer = frameReader.ReadBytes(packetLength);
                if (packetBuffer.Length == 0)
                {
                    continue;
                }

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
                    Console.WriteLine($"Packet decode/handle error ({packetLength} bytes): {exception}");
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
            packet.Serialize(packetBufferStream);

            var packetData = packetBufferStream.GetProcessedBytes();


            frameWriter.WriteVarInt(packetData.Length);
            frameWriter.WriteBytes(packetData.Span);
        }

        Console.WriteLine(Convert.ToHexString(frameWriter.GetProcessedBytes()));
        SendFrame(connection, frameWriter.GetProcessedBytes(), compression);
    }

    private void SendFrame(NetworkConnection connection, ReadOnlySpan<byte> frame, CompressionMethod? compression)
    {
        CompressionMethod method = compression ?? _server.Options.CompressionMethod;

        if (method != CompressionMethod.None && frame.Length < _server.Options.CompressionThreshold)
        {
            method = CompressionMethod.None;
        }

        if (method != CompressionMethod.Zlib)
        {
            SendRakNetFrame(connection, frame, method);
            return;
        }

        byte[] compressedBuffer = ArrayPool<byte>.Shared.Rent(frame.Length + 1024 * 1024);

        try
        {
            int compressedLength = Compress(frame, compressedBuffer);
            SendRakNetFrame(connection, compressedBuffer.AsSpan(0, compressedLength), method);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(compressedBuffer);
        }
    }

    private static void SendRakNetFrame(NetworkConnection connection, ReadOnlySpan<byte> payload, CompressionMethod compression)
    {
        int headerSize = compression == CompressionMethod.NotPresent ? 1 : 2;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(payload.Length + headerSize);

        try
        {
            buffer[0] = 0xFE;

            if (compression != CompressionMethod.NotPresent)
            {
                buffer[1] = (byte)compression;
            }

            payload.CopyTo(buffer.AsSpan(headerSize));

            connection.SendPacket(
                buffer.AsSpan(0, payload.Length + headerSize),
                Reliability.ReliableOrdered);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static int Compress(ReadOnlySpan<byte> input, byte[] output)
    {
        using MemoryStream stream = new(output);

        using (DeflateStream deflate = new(stream, CompressionLevel.Optimal, true))
        {
            deflate.Write(input);
        }

        return (int)stream.Position;
    }

    private static int Decompress(ReadOnlySpan<byte> input, byte[] output)
    {
        byte[] inputBuffer = ArrayPool<byte>.Shared.Rent(input.Length);

        try
        {
            input.CopyTo(inputBuffer);

            using MemoryStream stream = new(inputBuffer, 0, input.Length);
            using DeflateStream deflate = new(stream, CompressionMode.Decompress);

            int total = 0;

            while (total < output.Length)
            {
                int read = deflate.Read(output, total, output.Length - total);
                if (read == 0)
                {
                    break;
                }

                total += read;
            }

            return total;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(inputBuffer);
        }
    }
}
