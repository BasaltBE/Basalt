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
using BasaltBE.Basalt.Network.Events;
using BasaltBE.Basalt.Network.Handlers;
using BasaltBE.Basalt.Network.Login;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Logging;
using BasaltBE.NetherNet;
using BasaltBE.NetherNet.Enums;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Packets;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.Network;

public sealed class NetworkServer : IDisposable {
    private const int MaximumBatchLength = 4 * 1024 * 1024;
    internal const ushort CompressionThreshold = 512;
    private static readonly ConcurrentDictionary<Type, uint> packetIds = [];
    private readonly LoginAuthenticator authenticator = new();
    private readonly NethernetServer nethernet;
    // For now players live here, they will be moved to per world
    private readonly ConcurrentDictionary<NethernetPeer, Player> players = [];
    private readonly ConcurrentDictionary<NethernetPeer, byte> compressionPeers = [];

    public readonly EventEmitter Events = new();

    public NetworkServer(NethernetServerOptions options, IServerIdentity? identity = null) {
        nethernet = new NethernetServer(options, identity);
        Events.Subscribe(PacketEvents.RequestNetworkSettings, new RequestNetworkSettingsHandler(this).Handle);
        Events.Subscribe(PacketEvents.Login, new LoginHandler(this, authenticator).Handle);
        Events.Subscribe(PacketEvents.ClientToServerHandshake, new ClientToServerHandshakeHandler().Handle);
    }

    public ulong NetworkId => nethernet.NetworkId;

    public void Start() {
        using ProfilerZone zone = Profiler.BeginZone("Network.Start");
        authenticator.Start();
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
            throw new InvalidOperationException("Player is not connected.");
        SendPacket(player.Peer, player, packet, compress, method);
    }

    internal bool CompressionNegotiated(NethernetPeer peer) => compressionPeers.ContainsKey(peer);

    internal void SetCompression(NethernetPeer peer) => compressionPeers.TryAdd(peer, 0);

    internal void SendPacket(
        NethernetPeer peer,
        DataPacket packet,
        bool compress = true,
        PacketCompressionAlgorithm method = PacketCompressionAlgorithm.ZLib
    ) => SendPacket(peer, null, packet, compress, method);

    internal void AddPlayer(Player player) {
        using ProfilerZone zone = Profiler.BeginZone("Network.AddPlayer");
        if (!players.TryAdd(player.Peer, player))
            throw new InvalidDataException("Peer already has a Bedrock player.");
    }

    internal void RemovePlayer(NethernetPeer peer) {
        using ProfilerZone zone = Profiler.BeginZone("Network.RemovePlayer");
        if (players.TryRemove(peer, out Player? player)) player.Dispose();
    }

    public void Dispose() {
        using ProfilerZone zone = Profiler.BeginZone("Network.Dispose");
        nethernet.Dispose();
        authenticator.Dispose();
        foreach (Player player in players.Values) player.Dispose();
        players.Clear();
        compressionPeers.Clear();
    }

    internal void OnMessage(NethernetMessage message) {
        using ProfilerZone messageZone = Profiler.BeginZone("Network.OnMessage");
        if (message.Connected) {
            Logger.Scoped("Network").Info($"Peer {message.Peer.ConnectionId} connected.");
            return;
        }

        if (message.Disconnected) {
            RemovePlayer(message.Peer);
            compressionPeers.TryRemove(message.Peer, out _);
            Logger.Scoped("Network").Info($"Peer {message.Peer.ConnectionId} disconnected.");
            return;
        }

        Player? player = players.GetValueOrDefault(message.Peer);
        byte[]? decrypted = null;
        byte[]? decompressed = null;
        int decryptedLength = 0;
        bool compressionNegotiated = player is not null || CompressionNegotiated(message.Peer);
        try {
            ReadOnlyMemory<byte> payload = message.Payload;
            if (player?.Encryption is BedrockEncryption { Enabled: true } encryption) {
                if (payload.Length < 9)
                    throw new InvalidDataException("Encrypted Bedrock packet is too short.");
                decrypted = ArrayPool<byte>.Shared.Rent(payload.Length - 8);
                using (Profiler.BeginZone("Network.Decrypt"))
                    decryptedLength = encryption.Decrypt(payload.Span, decrypted.AsSpan(0, payload.Length - 8));
                payload = decrypted.AsMemory(0, decryptedLength);
            }

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

            bool handshakeReceivedInBatch = false;
            int offset = 0;
            while (offset < batch.Length) {
                using ProfilerZone packetZone = Profiler.BeginZone("Network.ReadPacket");
                if (handshakeReceivedInBatch)
                    throw new InvalidDataException("Client sent packets after its encryption handshake in the same batch.");

                BinaryReader reader = new(batch.Span, ref offset);
                uint packetLength = reader.ReadVarUInt();
                if (packetLength == 0 || packetLength > reader.Remaining)
                    throw new InvalidDataException("Bedrock packet length is invalid.");
                int packetEnd = checked(offset + (int)packetLength);
                BinaryReader packetReader = new(batch.Span[..packetEnd], ref offset);
                uint packetId = packetReader.ReadVarUInt();
                ReadOnlyMemory<byte> packetPayload = batch.Slice(offset, packetEnd - offset);
                offset = packetEnd;

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
                if (packetId == PacketEvents.ClientToServerHandshake.PacketId) {
                    handshakeReceivedInBatch = true;
                }
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
            ReadOnlySpan<byte> prefix = message.Payload.Span[..Math.Min(8, message.Payload.Length)];
            Logger.Scoped("Network").Error(
                $"Invalid Bedrock packet from {message.Peer.ConnectionId}: {error.Message} " +
                $"(compression: {(compressionNegotiated ? PacketCompressionAlgorithm.ZLib.ToString() : "not negotiated")}, payload prefix: {Convert.ToHexString(prefix)})."
            );
        }
        finally {
            if (decrypted is not null) {
                CryptographicOperations.ZeroMemory(decrypted.AsSpan(0, decryptedLength));
                ArrayPool<byte>.Shared.Return(decrypted);
            }
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
        PacketCompressionAlgorithm method
    ) {
        using ProfilerZone sendZone = Profiler.BeginZone("Network.SendPacket");
        ArgumentNullException.ThrowIfNull(packet);
        uint packetId = GetPacketId(packet);

        using Lock.Scope sync = player is null ? default : player.Sync.EnterScope();
        if (player?.Disposed == true) throw new ObjectDisposedException(nameof(Player));
        int capacity = packet switch {
            LoginPacket login => checked(login.ConnectionRequest.Length + 16),
            ServerToClientHandshakePacket handshake => Encoding.UTF8.GetByteCount(handshake.HandshakeWebToken) + 10,
            ResourcePackChunkDataPacket chunk => checked(chunk.ChunkData.Length + Encoding.UTF8.GetByteCount(chunk.ResourceName) + 32),
            ResourcePackDataInfoPacket info => checked(info.FileHash.Length + Encoding.UTF8.GetByteCount(info.ResourceName) + 32),
            _ => 256
        };

        using BinaryStream packetStream = BinaryStream.Rent(capacity);
        using BinaryStream batchStream = BinaryStream.Rent(capacity + 5);
        byte[]? markedBatch = null;
        byte[]? encrypted = null;
        int encryptedLength = 0;
        try {
            using (Profiler.BeginZone("Network.SerializePacket")) {
                BinaryWriter packetWriter = packetStream.GetWriter();
                packetWriter.WriteVarUInt(packetId);
                packet.Serialize(ref packetWriter);
            }
            int packetLength = packetStream.Offset;
            if (packetLength > MaximumBatchLength - 5)
                throw new InvalidDataException("Bedrock packet batch exceeds the configured limit.");

            BinaryWriter batchWriter = batchStream.GetWriter();
            batchWriter.WriteVarUInt((uint)packetLength);
            batchWriter.WriteBytes(packetStream.Buffer.Span[..packetLength]);
            ReadOnlyMemory<byte> payload = batchStream.Buffer[..batchStream.Offset];
            bool compressionNegotiated = player is not null || CompressionNegotiated(peer);

            if (compressionNegotiated && compress) {
                using ProfilerZone compressionZone = Profiler.BeginZone("Network.CompressPacket");
                if (method == PacketCompressionAlgorithm.Snappy)
                    throw new NotSupportedException("Bedrock Snappy compression is not supported.");
                if (method == PacketCompressionAlgorithm.ZLib && payload.Length >= CompressionThreshold) {
                    using MemoryStream compressedStream = new();
                    using (DeflateStream deflater = new(compressedStream, CompressionLevel.Fastest, true))
                        deflater.Write(payload.Span);
                    byte[] compressed = compressedStream.ToArray();
                    if (compressed.Length < payload.Length) {
                        markedBatch = GC.AllocateUninitializedArray<byte>(compressed.Length + 1);
                        markedBatch[0] = (byte)method;
                        compressed.CopyTo(markedBatch.AsSpan(1));
                    }
                    CryptographicOperations.ZeroMemory(compressed);
                }

                if (markedBatch is null) {
                    markedBatch = GC.AllocateUninitializedArray<byte>(payload.Length + 1);
                    markedBatch[0] = unchecked((byte)PacketCompressionAlgorithm.None);
                    payload.Span.CopyTo(markedBatch.AsSpan(1));
                }
                payload = markedBatch;
            }

            if (player?.Encryption is BedrockEncryption { Enabled: true } encryptionContext) {
                using ProfilerZone encryptionZone = Profiler.BeginZone("Network.EncryptPacket");
                encryptedLength = checked(payload.Length + 8);
                encrypted = ArrayPool<byte>.Shared.Rent(encryptedLength);
                int written = encryptionContext.Encrypt(payload.Span, encrypted.AsSpan(0, encryptedLength));
                payload = encrypted.AsMemory(0, written);
            }

            using (Profiler.BeginZone("Network.TransportSend"))
                nethernet.Send(peer, payload, MessageReliability.Reliable);
        }
        finally {
            if (encrypted is not null) {
                CryptographicOperations.ZeroMemory(encrypted.AsSpan(0, encryptedLength));
                ArrayPool<byte>.Shared.Return(encrypted);
            }
            if (markedBatch is not null) CryptographicOperations.ZeroMemory(markedBatch);
            CryptographicOperations.ZeroMemory(packetStream.Buffer.Span[..packetStream.Offset]);
            CryptographicOperations.ZeroMemory(batchStream.Buffer.Span[..batchStream.Offset]);
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
