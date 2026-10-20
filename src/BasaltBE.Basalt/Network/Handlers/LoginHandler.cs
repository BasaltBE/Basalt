using System.Collections.Concurrent;
using System.Security.Cryptography;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Events.Players;
using BasaltBE.Basalt.Network.Login;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Logging;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class LoginHandler(NetworkServer network) {
    private const int MaximumConcurrentAuthentications = 4;
    private readonly LoginAuthenticator authenticator = new();
    private readonly ConcurrentDictionary<NethernetPeer, byte> pending = [];
    private readonly ConcurrentQueue<LoginCompletion> completed = new();
    private int activeAuthentications;
    private int disposed;

    private readonly record struct LoginCompletion(NethernetPeer Peer, AuthenticatedLogin? Login, Exception? Error);

    private sealed record AuthenticationRequest(LoginHandler Handler, NethernetPeer Peer, byte[] ConnectionRequest);

    internal void Start() => authenticator.Start();

    public void Handle(NethernetPeer peer, Player? player, LoginPacket packet) {
        using ProfilerZone zone = Profiler.BeginZone("Network.Login");
        if (player is not null || !network.CompressionNegotiated(peer) ||
            packet.ClientNetworkVersion < BedrockProtocol.MinimumProtocolVersion ||
            packet.ClientNetworkVersion > BedrockProtocol.ProtocolVersion)
            throw new InvalidDataException("Bedrock Login arrived outside the expected connection state.");

        QueueLoginAuthentication(peer, packet.ConnectionRequest);
    }

    private void QueueLoginAuthentication(NethernetPeer peer, ReadOnlyMemory<byte> connectionRequest) {
        if (Volatile.Read(ref disposed) != 0)
            return;
        if (Interlocked.Increment(ref activeAuthentications) > MaximumConcurrentAuthentications) {
            Interlocked.Decrement(ref activeAuthentications);
            network.Disconnect(peer);
            return;
        }
        if (!pending.TryAdd(peer, 0)) {
            Interlocked.Decrement(ref activeAuthentications);
            network.Disconnect(peer);
            return;
        }

        byte[] request = connectionRequest.ToArray();
        AuthenticationRequest work = new(this, peer, request);
        if (ThreadPool.QueueUserWorkItem(static state => {
            AuthenticationRequest request = (AuthenticationRequest)state!;
            LoginHandler handler = request.Handler;
            AuthenticatedLogin? login = null;
            Exception? error = null;
            try {
                login = handler.authenticator.Authenticate(request.ConnectionRequest);
            }
            catch (Exception authenticationError) {
                error = authenticationError;
            }
            finally {
                CryptographicOperations.ZeroMemory(request.ConnectionRequest);
                Interlocked.Decrement(ref handler.activeAuthentications);
            }

            if (Volatile.Read(ref handler.disposed) != 0) {
                if (login is { } authenticated)
                    CryptographicOperations.ZeroMemory(authenticated.Skin);
                return;
            }

            handler.completed.Enqueue(new LoginCompletion(request.Peer, login, error));
        }, work))
            return;

        pending.TryRemove(peer, out _);
        Interlocked.Decrement(ref activeAuthentications);
        CryptographicOperations.ZeroMemory(request);
        throw new InvalidOperationException("Could not queue Bedrock login authentication.");
    }

    internal void ProcessCompletions(World world) {
        if (!ReferenceEquals(world, network.Server.GetWorld()))
            return;

        for (int processed = 0;
             processed < MaximumConcurrentAuthentications && completed.TryDequeue(out LoginCompletion completion);
             processed++) {
            if (!pending.TryRemove(completion.Peer, out _) || !network.PeerConnected(completion.Peer)) {
                if (completion.Login is { } abandoned)
                    CryptographicOperations.ZeroMemory(abandoned.Skin);
                continue;
            }

            if (completion.Error is not null) {
                Logger.Scoped("Network").Warn(
                    $"Login failed for {completion.Peer.ConnectionId}: {completion.Error.Message}"
                );
                network.Disconnect(completion.Peer);
                continue;
            }

            try {
                Complete(completion.Peer, completion.Login!.Value);
            }
            catch (Exception error) when (
                error is InvalidDataException or InvalidOperationException or ArgumentException or ObjectDisposedException)
            {
                Logger.Scoped("Network").Warn(
                    $"Login failed for {completion.Peer.ConnectionId}: {error.Message}"
                );
                network.Disconnect(completion.Peer);
            }
        }
    }

    internal void Disconnected(NethernetPeer peer) => pending.TryRemove(peer, out _);

    private void Complete(NethernetPeer peer, AuthenticatedLogin login) {
        World world = network.Server.GetWorld();
        Dimension dimension = world.GetDimension("minecraft:overworld")
            ?? throw new InvalidOperationException("The default world has no overworld dimension.");
        Player player = new(
            network,
            world,
            dimension,
            peer,
            login.Username,
            login.Xuid,
            login.Uuid,
            login.DeviceOS,
            login.DeviceName,
            login.Skin
        );

        try {
            network.AddPlayer(player);
            network.SendPacket(player, new PlayStatusPacket { Status = PlayStatus.LoginSuccess });
            network.Server.Events.Emit(new PlayerJoinEvent(player));
            Logger.Scoped("Network").Info($"Player {player.Username} Logged in.");
        }
        catch {
            network.RemovePlayer(peer);
            player.Dispose();
            throw;
        }
    }

    internal void Dispose() {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;
        authenticator.Dispose();
        pending.Clear();
        completed.Clear();
    }
}
