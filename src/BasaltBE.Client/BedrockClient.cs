using System.Collections.Concurrent;
using System.IO.Compression;
using System.Reflection;
using Basalt.Binary;
using BasaltBE.Client.Auth;
using BasaltBE.Client.Events;
using BasaltBE.Client.Login;
using BasaltBE.Client.Network.Handlers;
using BasaltBE.NetherNet;
using BasaltBE.NetherNet.Enums;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Attributes;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Client;

public sealed class BedrockClient : IAsyncDisposable {
    #region Fields

    private readonly BedrockClientOptions options;
    private readonly HttpClient authenticationHttp = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly BedrockAuthenticator authenticator;
    private readonly NethernetClient transport;
    private readonly CancellationTokenSource lifetime = new();
    internal readonly TaskCompletionSource SpawnCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ConcurrentQueue<DataPacket> outgoingPackets = new();
    private readonly SemaphoreSlim packetDispatch = new(1, 1);
    private readonly NetworkSettingsHandler networkSettingsHandler;
    internal BedrockNetworkSettings? Compression;
    private Task? receiveTask;
    private int connected;
    private int disposed;

    #endregion

    #region Public API

    public BedrockAccount Account;
    public BedrockNetworkSettings NetworkSettings;
    public readonly EventEmitter Events = new();
    public StartGamePacket StartGame = new();
    public int ChunkRadius;
    public bool Spawned;

    public BedrockClient(BedrockClientOptions options) {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.ServerUri);
        if (options.ProtocolVersion < 0 || options.ServerUri.Scheme is not ("http" or "https"))
            throw new ArgumentOutOfRangeException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.AuthenticationCachePath))
            throw new ArgumentException("Authentication cache path is required.", nameof(options));

        this.options = options;
        authenticator = new BedrockAuthenticator(authenticationHttp);
        transport = new NethernetClient(new NethernetClientOptions {
            SignalingUri = options.ServerUri,
            ClientVersion = Protocol.BedrockProtocol.MinecraftVersion,
            ProtocolVersion = options.ProtocolVersion,
            Platform = 8
        });

        networkSettingsHandler = new NetworkSettingsHandler(this, options.ProtocolVersion);
        Events.Subscribe(PacketEvents.NetworkSettings, networkSettingsHandler.Handle);
        Events.Subscribe(PacketEvents.PlayStatus, new PlayStatusHandler(this).Handle);
        Events.Subscribe(PacketEvents.ResourcePacksInfo, new ResourcePacksInfoHandler(this).Handle);
        Events.Subscribe(PacketEvents.ResourcePackStack, new ResourcePackStackHandler(this).Handle);
        Events.Subscribe(PacketEvents.StartGame, new StartGameHandler(this).Handle);
        Events.Subscribe(PacketEvents.ChunkRadiusUpdated, new ChunkRadiusUpdatedHandler(this).Handle);
        Events.Subscribe(PacketEvents.ServerToClientHandshake, new ServerToClientHandshakeHandler(this).Handle);
        Events.Subscribe(PacketEvents.Disconnect, new DisconnectHandler().Handle);
    }

    public async Task<BedrockNetworkSettings> ConnectAsync(
        Action<MicrosoftDeviceCode> showDeviceCode,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(showDeviceCode);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        if (Interlocked.CompareExchange(ref connected, -1, 0) != 0) {
            throw new InvalidOperationException("Bedrock client has already connected.");
        }

        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            lifetime.Token
        );
        try {
            BedrockAuthentication authentication = await authenticator.AuthenticateAsync(
                options.AuthenticationCachePath,
                options.ServerUri,
                showDeviceCode,
                linked.Token
            ).ConfigureAwait(false);
            Account = authentication.Account;
            networkSettingsHandler.SetAuthentication(authentication.IdentityToken, authentication.ClientToken);

            await transport.ConnectAsync(
                linked.Token,
                offer => BedrockNethernetIdentity.AddAssertion(
                    offer,
                    authentication.IdentityToken,
                    authentication.PrivateKey
                )
            ).ConfigureAwait(false);
            receiveTask = ReceivePacketsAsync(lifetime.Token);
            await SendPacketAsync(
                new RequestNetworkSettingsPacket { ClientNetworkVersion = options.ProtocolVersion },
                linked.Token
            ).ConfigureAwait(false);

            Task completed = await Task.WhenAny(SpawnCompletion.Task, receiveTask).ConfigureAwait(false);
            if (completed == receiveTask) {
                await receiveTask.ConfigureAwait(false);
                throw new IOException("Bedrock connection closed before the player spawned.");
            }

            await SpawnCompletion.Task.WaitAsync(linked.Token).ConfigureAwait(false);
            await packetDispatch.WaitAsync(linked.Token).ConfigureAwait(false);
            packetDispatch.Release();
            Volatile.Write(ref connected, 1);
            return NetworkSettings;
        }
        catch {
            Volatile.Write(ref connected, 2);
            lifetime.Cancel();
            throw;
        }
    }

    public async ValueTask DisposeAsync() {
        if (Interlocked.Exchange(ref disposed, 1) != 0) {
            return;
        }

        lifetime.Cancel();
        try {
            if (receiveTask is not null) {
                await receiveTask.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        finally {
            await transport.DisposeAsync().ConfigureAwait(false);
            authenticationHttp.Dispose();
            lifetime.Dispose();
            packetDispatch.Dispose();
        }
    }

    internal void QueuePacket(DataPacket packet) => outgoingPackets.Enqueue(packet);

    #endregion

    #region Network receive and send

    private async Task ReceivePacketsAsync(CancellationToken cancellationToken) {
        while (true) {
            NethernetMessage message = await transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            if (message.Disconnected) {
                return;
            }

            if (message.Connected || message.Payload.IsEmpty) {
                continue;
            }

            await DispatchPacketsAsync(DecompressBatch(message.Payload), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DispatchPacketsAsync(ReadOnlyMemory<byte> batch, CancellationToken cancellationToken) {
        int offset = 0;
        while (offset < batch.Length) {
            BinaryReader lengthReader = new(batch.Span, ref offset);
            uint packetLength = lengthReader.ReadVarUInt();
            if (packetLength == 0 || packetLength > lengthReader.Remaining) {
                throw new InvalidDataException("Bedrock packet length is invalid.");
            }

            int packetEnd = checked(offset + (int)packetLength);
            BinaryReader packetReader = new(batch.Span[..packetEnd], ref offset);
            uint packetId = packetReader.ReadVarUInt();
            ReadOnlyMemory<byte> packetPayload = batch[offset..packetEnd];
            await packetDispatch.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                Events.Emit(packetId, packetPayload.Span);
                while (outgoingPackets.TryDequeue(out DataPacket? outgoingPacket)) {
                    await SendPacketAsync(outgoingPacket, cancellationToken).ConfigureAwait(false);
                }
            }
            finally {
                packetDispatch.Release();
            }

            offset = packetEnd;
        }
    }

    private async ValueTask SendPacketAsync(DataPacket packet, CancellationToken cancellationToken) {
        PacketIdAttribute attribute = packet.GetType().GetCustomAttribute<PacketIdAttribute>()
            ?? throw new InvalidOperationException($"{packet.GetType().Name} has no packet ID.");
        int capacity = packet is LoginPacket login ? checked(login.ConnectionRequest.Length + 16) : 64;
        using BinaryStream packetStream = BinaryStream.Rent(capacity);
        using BinaryStream batchStream = BinaryStream.Rent(capacity + 5);

        BinaryWriter packetWriter = packetStream.GetWriter();
        packetWriter.WriteVarUInt((uint)attribute.Id);
        packet.Serialize(ref packetWriter);

        BinaryWriter batchWriter = batchStream.GetWriter();
        batchWriter.WriteVarUInt((uint)packetStream.Offset);
        batchWriter.WriteBytes(packetStream.Buffer.Span[..packetStream.Offset]);
        ReadOnlyMemory<byte> batch = batchStream.Buffer[..batchStream.Offset];
        if (Compression is not null) {
            batch = MarkBatch(batch);
        }

        await transport.SendAsync(batch, MessageReliability.Reliable, cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region Compression

    private ReadOnlyMemory<byte> MarkBatch(ReadOnlyMemory<byte> batch) {
        BedrockNetworkSettings settings = Compression
            ?? throw new InvalidOperationException("Bedrock compression settings have not been negotiated.");
        if (settings.CompressionAlgorithm == PacketCompressionAlgorithm.Snappy) {
            throw new NotSupportedException("Bedrock Snappy compression is not supported.");
        }

        if (settings.CompressionAlgorithm == PacketCompressionAlgorithm.ZLib &&
            batch.Length >= settings.CompressionThreshold) {
            using MemoryStream compressed = new();
            using (DeflateStream deflate = new(compressed, CompressionLevel.Fastest, true))
                deflate.Write(batch.Span);
            byte[] result = new byte[compressed.Length + 1];
            result[0] = (byte)PacketCompressionAlgorithm.ZLib;
            compressed.GetBuffer().AsSpan(0, (int)compressed.Length).CopyTo(result.AsSpan(1));
            return result;
        }

        byte[] uncompressed = new byte[batch.Length + 1];
        uncompressed[0] = unchecked((byte)PacketCompressionAlgorithm.None);
        batch.Span.CopyTo(uncompressed.AsSpan(1));
        return uncompressed;
    }

    private static ReadOnlyMemory<byte> DecompressBatch(ReadOnlyMemory<byte> payload) {
        byte marker = payload.Span[0];
        if (marker == unchecked((byte)PacketCompressionAlgorithm.None)) {
            return payload[1..];
        }

        if (marker != (byte)PacketCompressionAlgorithm.ZLib) {
            throw new InvalidDataException($"Unsupported Bedrock compression marker 0x{marker:X2}.");
        }

        using MemoryStream compressed = new(payload[1..].ToArray(), false);
        using DeflateStream deflate = new(compressed, CompressionMode.Decompress);
        using MemoryStream batch = new();
        deflate.CopyTo(batch);
        return batch.ToArray();
    }

    #endregion

}
