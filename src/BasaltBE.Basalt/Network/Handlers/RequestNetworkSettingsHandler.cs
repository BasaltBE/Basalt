using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Logging;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using Org.BouncyCastle.Tls.Crypto.Impl.BC;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class RequestNetworkSettingsHandler(NetworkServer network) {
    public void Handle(NethernetPeer peer, Player? player, RequestNetworkSettingsPacket packet) {
        using ProfilerZone zone = Profiler.BeginZone("Network.RequestNetworkSettings");
        if (player is not null || network.CompressionNegotiated(peer))
            throw new InvalidDataException("RequestNetworkSettingsHandler: Player already exists.");

        if (packet.ClientNetworkVersion < BedrockProtocol.MinimumProtocolVersion ||
            packet.ClientNetworkVersion > BedrockProtocol.ProtocolVersion) {
            bool outdatedClient = packet.ClientNetworkVersion < BedrockProtocol.MinimumProtocolVersion;
            string message = outdatedClient
                ? $"Outdated client. Please use Minecraft {BedrockProtocol.MinecraftVersion}."
                : $"Outdated server. Please use Minecraft {BedrockProtocol.MinecraftVersion}.";

            network.SendPacket(peer, new DisconnectPacket {
                Reason = outdatedClient ? DisconnectFailReason.OutdatedClient : DisconnectFailReason.OutdatedServer,
                Message = message,
                FilteredMessage = message
            }, compress: false);
            Logger.Scoped("Network").Warn($"Rejected unsupported Bedrock protocol {packet.ClientNetworkVersion} from {peer.ConnectionId}.");
            return;
        }

        network.SendPacket(peer, new NetworkSettingsPacket {
            CompressionThreshold = NetworkServer.CompressionThreshold,
            CompressionAlgorithm = PacketCompressionAlgorithm.ZLib,
            ClientThrottleEnabled = false,
            ClientThrottleScalar = 0,
            ClientThrottleThreshold = 0,
        }, compress: false);
        network.SetCompression(peer);
        Logger.Scoped("Network").Debug($"Bedrock protocol {packet.ClientNetworkVersion} negotiated with {peer.ConnectionId}.");
    }
}
