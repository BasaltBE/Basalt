using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Channels;
using BasaltBE.NetherNet.Connections;
using BasaltBE.NetherNet.Discovery;
using BasaltBE.NetherNet.Enums;
using BasaltBE.NetherNet.Signaling;
using BasaltBE.NetherNet.Types;
using BasaltBE.Logging;

namespace BasaltBE.NetherNet;

public sealed class NethernetServer : IAsyncDisposable {
    private static readonly ScopedLogger Log = Logger.Scoped("NetherNet");
    private readonly NethernetServerOptions options;
    private readonly IServerIdentity identity;
    private readonly bool ownsIdentity;
    private readonly Channel<NethernetMessage> incoming;
    private readonly ConcurrentDictionary<NethernetPeer, NethernetConnection> connections = new();
    private readonly ConcurrentDictionary<NethernetPeer, Task> monitors = new();
    private readonly CancellationTokenSource cancellation = new();
    private readonly SemaphoreSlim peerCapacity;
    private readonly Lock disposeSync = new();
    private CancellationTokenRegistration startupCancellation;
    private Task? disposeTask;
    private LanDiscovery? discovery;
    private HttpSignalingListener? http;
    private int started;
    private int disposed;

    public NethernetServer(NethernetServerOptions options, IServerIdentity? identity = null) {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Advertisement);
        if (options.MaximumPeers < 1 || options.MaximumQueuedMessages < 1 || options.MaximumSdpLength < 1 ||
            options.MaximumPacketLength < 1 || options.ReliableFragmentLength is < 1 or > 262_143 ||
            options.UnreliableFragmentLength is < 1 or > 262_143 || options.NegotiationTimeout <= TimeSpan.Zero ||
            options.RequestTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options));
        this.options = options;
        this.identity = identity ?? new ServerIdentity(options.IdentityKeyPath);
        ownsIdentity = identity is null;
        peerCapacity = new SemaphoreSlim(options.MaximumPeers, options.MaximumPeers);
        incoming = Channel.CreateBounded<NethernetMessage>(new BoundedChannelOptions(options.MaximumQueuedMessages) {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ulong NetworkId { get; private set; }

    public async ValueTask StartAsync(CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        if (Interlocked.Exchange(ref started, 1) != 0) throw new InvalidOperationException("NetherNet server has already started.");
        try {
            NetworkId = options.NetworkId == 0 ? RandomNetworkId() : options.NetworkId;
            discovery = new LanDiscovery(options, identity, NetworkId, AcceptLanAsync);
            http = new HttpSignalingListener(options, identity, AcceptAsync);
            startupCancellation = cancellationToken.Register(static state => ((NethernetServer)state!).StopOnCancellation(), this);
            if (cancellationToken.IsCancellationRequested) {
                startupCancellation.Dispose();
                await DisposeAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        catch {
            await DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    public ValueTask<NethernetMessage> ReceiveAsync(CancellationToken cancellationToken = default) => incoming.Reader.ReadAsync(cancellationToken);

    public ValueTask SendAsync(NethernetPeer peer, ReadOnlyMemory<byte> payload, MessageReliability reliability = MessageReliability.Reliable, CancellationToken cancellationToken = default) {
        if (!connections.TryGetValue(peer, out NethernetConnection? connection)) throw new InvalidOperationException("Peer is not connected.");
        return connection.SendAsync(payload, reliability, cancellationToken);
    }

    public ValueTask DisposeAsync() {
        lock (disposeSync)
            return new ValueTask(disposeTask ??= DisposeCoreAsync());
    }

    private async Task DisposeCoreAsync() {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        cancellation.Cancel();
        startupCancellation.Dispose();
        incoming.Writer.TryComplete();
        if (http is not null) await http.DisposeAsync().ConfigureAwait(false);
        if (discovery is not null) await discovery.DisposeAsync().ConfigureAwait(false);
        await Task.WhenAll(connections.Values.Select(connection => connection.DisposeAsync().AsTask())).ConfigureAwait(false);
        await Task.WhenAll(monitors.Values).ConfigureAwait(false);
        if (ownsIdentity) identity.Dispose();
        peerCapacity.Dispose();
        cancellation.Dispose();
    }

    private void StopOnCancellation() {
        _ = DisposeAsync().AsTask().ContinueWith(task =>
            Log.Error($"NetherNet shutdown failed: {task.Exception?.GetBaseException().Message}"),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private ValueTask AcceptAsync(NethernetPeer peer, NethernetConnection connection) {
        if (!peerCapacity.Wait(0)) throw new InvalidOperationException("NetherNet server is at capacity.");
        if (!connections.TryAdd(peer, connection)) {
            peerCapacity.Release();
            throw new InvalidOperationException("Duplicate NetherNet peer.");
        }
        Lock eventSync = new();
        int connectionState = 0;
        bool PublishConnected() {
            lock (eventSync) {
                if (connectionState == 1) return true;
                if (connectionState == -1) return false;
                if (!incoming.Writer.TryWrite(new NethernetMessage(peer, MessageReliability.Reliable, ReadOnlyMemory<byte>.Empty, Connected: true))) {
                    connectionState = -1;
                    return false;
                }
                connectionState = 1;
                return true;
            }
        }

        connection.MessageHandler = message => {
            lock (eventSync) {
                if (!PublishConnected() || !incoming.Writer.TryWrite(message))
                    connection.Close(new IOException("NetherNet receive queue is full."));
            }
        };
        Task monitor = ObserveConnectionAsync(peer, connection, PublishConnected, eventSync, () => connectionState == 1);
        monitors[peer] = monitor;
        _ = monitor.ContinueWith(completed => {
            if (monitors.TryGetValue(peer, out Task? current) && ReferenceEquals(current, completed))
                monitors.TryRemove(peer, out _);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return ValueTask.CompletedTask;
    }

    private async Task ObserveConnectionAsync(NethernetPeer peer, NethernetConnection connection, Func<bool> publishConnected, Lock eventSync, Func<bool> wasConnected) {
        try {
            await connection.ChannelsReady.WaitAsync(options.NegotiationTimeout, cancellation.Token).ConfigureAwait(false);
            if (!publishConnected())
                throw new IOException("NetherNet event queue is full.");
            Exception? failure = await connection.Completion.ConfigureAwait(false);
            if (failure is not null and not ObjectDisposedException) Log.Warn($"Peer {peer.NetworkId}/{peer.ConnectionId} closed: {failure.Message}");
        }
        catch (Exception error) when (error is OperationCanceledException or TimeoutException or ChannelClosedException or IOException) {
            if (error is not OperationCanceledException || !cancellation.IsCancellationRequested)
                Log.Error($"Peer {peer.NetworkId}/{peer.ConnectionId} failed: {error.Message}");
        }
        finally {
            connections.TryRemove(peer, out _);
            bool connected;
            lock (eventSync) connected = wasConnected();
            try {
                if (connected)
                    await incoming.Writer.WriteAsync(new NethernetMessage(peer, MessageReliability.Unreliable, ReadOnlyMemory<byte>.Empty, Disconnected: true), cancellation.Token).ConfigureAwait(false);
            }
            catch (Exception error) when (error is ChannelClosedException or OperationCanceledException) { }
            finally { peerCapacity.Release(); }
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    private ValueTask AcceptLanAsync(NethernetPeer peer, NethernetConnection connection) => AcceptAsync(peer, connection);

    private static ulong RandomNetworkId() {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        RandomNumberGenerator.Fill(bytes);
        return System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes);
    }
}

