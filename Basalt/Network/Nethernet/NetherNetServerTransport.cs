using System.Buffers;
using System.Collections.Concurrent;
using Basalt.Core.Nethernet;

namespace Basalt.Core.Network.Nethernet;

internal sealed class NetherNetServerTransport : IDisposable {
    private readonly NetworkHandler _network;
    private readonly ServerIdentity _identity;
    private readonly NetherNetSignalingServer _signaling;
    private readonly ConcurrentDictionary<NetherNetPeer, NetworkConnection> _peers = new();
    private readonly ConcurrentDictionary<NetherNetPeer, NetherNetConnection> _unreliableChannels = new();
    private readonly BlockingCollection<(NetherNetConnection Connection, byte[] Payload, int Length)> _outgoing =
        new(new ConcurrentQueue<(NetherNetConnection Connection, byte[] Payload, int Length)>(), 4096);
    private readonly BlockingCollection<(NetherNetConnection Connection, byte[] Payload, int Length)> _unreliableOutgoing =
        new(new ConcurrentQueue<(NetherNetConnection Connection, byte[] Payload, int Length)>(), 4096);
    private readonly ConcurrentQueue<Action> _commands = new();
    private readonly AutoResetEvent _wake = new(false);
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Thread _thread;
    private bool _started;

    public NetherNetServerTransport(NetworkHandler network, ushort ipv4Port, ushort ipv6Port) {
        _network = network ?? throw new ArgumentNullException(nameof(network));
        _identity = ServerIdentity.LoadOrGenerate("nethernet-identity.pem");
        _signaling = new NetherNetSignalingServer(ipv4Port, ipv6Port, CreateAnswerAsync);
        _thread = new Thread(Run) { IsBackground = true, Name = "NetherNet" };
    }

    public void Start(CancellationToken cancellationToken) {
        if (_started) {
            throw new InvalidOperationException("The NetherNet transport is already running.");
        }

        _started = true;
        _signaling.Start(cancellationToken);
        _thread.Start();
    }

    public void Dispose() {
        if (!_started) {
            _identity.Dispose();
            _signaling.Dispose();
            _cancellation.Dispose();
            _outgoing.Dispose();
            _unreliableOutgoing.Dispose();
            _wake.Dispose();
            return;
        }

        _outgoing.CompleteAdding();
        _unreliableOutgoing.CompleteAdding();
        _cancellation.Cancel();
        _wake.Set();
        _thread.Join(1000);
        _signaling.StopAsync().GetAwaiter().GetResult();
        foreach (NetherNetPeer peer in _peers.Keys) {
            peer.Dispose();
        }

        _peers.Clear();
        _identity.Dispose();
        _signaling.Dispose();
        _cancellation.Dispose();
        _outgoing.Dispose();
        _unreliableOutgoing.Dispose();
        _wake.Dispose();
        _started = false;
    }

    private async Task<string?> CreateAnswerAsync(string _, string offer, CancellationToken cancellationToken) {
        NetherNetPeer peer = new(_identity);
        try {
            peer.ChannelOpened += connection => Attach(peer, connection);
            peer.Closed += () => Close(peer);
            string answer = await peer.AcceptOfferAsync(offer, cancellationToken).ConfigureAwait(false);
            return answer;
        }
        catch {
            peer.Dispose();
            throw;
        }
    }

    private void Attach(NetherNetPeer peer, NetherNetConnection netherConnection) {
        if (netherConnection.Channel != NetherNetChannel.Reliable) {
            _unreliableChannels[peer] = netherConnection;
            if (_peers.TryGetValue(peer, out NetworkConnection? existing)) {
                netherConnection.MessageReceived += payload => _network.EnqueueFrame(existing, payload);
            }

            return;
        }

        NetworkConnection? connection = null;
        connection = new NetworkConnection(
            (payload, _, _) => {
                EnqueueOutgoing(peer, netherConnection, payload, false);
            },
            () => {
                _commands.Enqueue(peer.Dispose);
                _wake.Set();
            },
            unreliableSend: (payload, _, _) => {
                if (_unreliableChannels.TryGetValue(peer, out NetherNetConnection? unreliableChannel)) {
                    EnqueueOutgoing(peer, unreliableChannel, payload, true);
                }
            },
            sendOwned: (payload, length, unreliable, _) => {
                NetherNetConnection target = unreliable &&
                    _unreliableChannels.TryGetValue(peer, out NetherNetConnection? unreliableChannel)
                    ? unreliableChannel
                    : netherConnection;
                EnqueueOutgoing(peer, target, payload, length, unreliable);
            });
        _peers[peer] = connection;
        netherConnection.MessageReceived += payload => _network.EnqueueFrame(connection, payload);
        netherConnection.Closed += () => _network.EnqueueDisconnection(connection);
        if (_unreliableChannels.TryGetValue(peer, out NetherNetConnection? unreliable)) {
            unreliable.MessageReceived += payload => _network.EnqueueFrame(connection, payload);
        }
    }

    private void Close(NetherNetPeer peer) {
        if (_peers.TryRemove(peer, out NetworkConnection? connection)) {
            _network.EnqueueDisconnection(connection);
        }

        _unreliableChannels.TryRemove(peer, out _);
    }

    private void EnqueueOutgoing(
        NetherNetPeer peer,
        NetherNetConnection connection,
        ReadOnlySpan<byte> payload,
        bool unreliable) {
        BlockingCollection<(NetherNetConnection Connection, byte[] Payload, int Length)> outgoing =
            unreliable ? _unreliableOutgoing : _outgoing;
        try {
            if (outgoing.TryAdd((connection, payload.ToArray(), payload.Length))) {
                _wake.Set();
                return;
            }
        }
        catch (InvalidOperationException) when (_outgoing.IsAddingCompleted) {
            return;
        }
        catch (ObjectDisposedException) {
            return;
        }

        if (unreliable) {
            return;
        }

        _commands.Enqueue(peer.Dispose);
        _wake.Set();
    }

    private void EnqueueOutgoing(
        NetherNetPeer peer,
        NetherNetConnection connection,
        byte[] payload,
        int length,
        bool unreliable) {
        BlockingCollection<(NetherNetConnection Connection, byte[] Payload, int Length)> outgoing =
            unreliable ? _unreliableOutgoing : _outgoing;
        try {
            if (outgoing.TryAdd((connection, payload, length))) {
                _wake.Set();
                return;
            }
        }
        catch (InvalidOperationException) when (_outgoing.IsAddingCompleted) {
            ArrayPool<byte>.Shared.Return(payload);
            return;
        }
        catch (ObjectDisposedException) {
            ArrayPool<byte>.Shared.Return(payload);
            return;
        }

        ArrayPool<byte>.Shared.Return(payload);
        if (unreliable) {
            return;
        }

        _commands.Enqueue(peer.Dispose);
        _wake.Set();
    }

    private void Run() {
        while (!_cancellation.IsCancellationRequested) {
            while (_commands.TryDequeue(out Action? command)) {
                try {
                    command();
                }
                catch (Exception exception) {
                    Logger.Warn($"NetherNet command failed: {exception.Message}");
                }
            }

            while (_outgoing.TryTake(out (NetherNetConnection Connection, byte[] Payload, int Length) outgoing)) {
                try {
                    outgoing.Connection.Send(outgoing.Payload.AsSpan(0, outgoing.Length));
                }
                catch (Exception exception) {
                    Logger.Warn($"NetherNet send failed: {exception.Message}");
                }
            }

            int unreliableSent = 0;
            while (unreliableSent < 256 &&
                   _unreliableOutgoing.TryTake(out (NetherNetConnection Connection, byte[] Payload, int Length) unreliable)) {
                try {
                    unreliable.Connection.Send(unreliable.Payload.AsSpan(0, unreliable.Length));
                }
                catch (Exception exception) {
                    Logger.Warn($"NetherNet send failed: {exception.Message}");
                }

                unreliableSent++;
            }

            if (_commands.IsEmpty && _outgoing.Count == 0 && _unreliableOutgoing.Count == 0) {
                _wake.WaitOne();
            }
        }
    }
}
