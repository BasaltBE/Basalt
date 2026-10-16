using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Events.Players;
using BasaltBE.Basalt.Network.Events;
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

internal sealed class LoginHandler(NetworkServer network, LoginAuthenticator authenticator) {
    public void Handle(NethernetPeer peer, Player? player, LoginPacket packet) {
        using ProfilerZone zone = Profiler.BeginZone("Network.Login");
        if (player is not null || !network.CompressionNegotiated(peer) ||
            packet.ClientNetworkVersion < BedrockProtocol.MinimumProtocolVersion ||
            packet.ClientNetworkVersion > BedrockProtocol.ProtocolVersion)
            throw new InvalidDataException("Bedrock Login arrived outside the expected connection state.");

        AuthenticatedLogin login = authenticator.Authenticate(packet.ConnectionRequest);
        World world = network.Server.GetWorld();
        Dimension dimension = world.GetDimension("minecraft:overworld")
            ?? throw new InvalidOperationException("The default world has no overworld dimension.");
        Player newPlayer = new(
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
            network.AddPlayer(newPlayer);
            network.SendPacket(newPlayer, new PlayStatusPacket { Status = PlayStatus.LoginSuccess });
            PlayerJoinEvent joined = new(newPlayer);
            network.Server.Events.Emit(joined);
            Logger.Scoped("Network").Info($"Authenticated Xbox player {newPlayer.Username} ({newPlayer.Xuid}) from {peer.ConnectionId}.");
        }
        catch {
            network.RemovePlayer(peer);
            newPlayer.Dispose();
            throw;
        }
    }

}
