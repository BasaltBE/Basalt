namespace Basalt.Core.Network.Handlers;

using Basalt.Core;

using BedrockProtocol.Enums;
using BedrockProtocol.Packets;

public static class RequestNetworkSettings {
    public static void Handle(Server server, NetworkConnection connection, BedrockProtocol.Packets.RequestNetworkSettingsPacket packet) {
        if (packet.ClientNetworkVersion != Constants.ProtocolVersion) {
            DisconnectFailReason reason = packet.ClientNetworkVersion < Constants.ProtocolVersion
                ? DisconnectFailReason.OutdatedClient
                : DisconnectFailReason.OutdatedServer;

            DisconnectPacket disconnect = new() {
                Reason = reason,
            };

            Logger.Warn($"Session(0) failed due to {reason.ToString()}");
            server.Network.QueuePacket(connection, disconnect, Protocol.Enums.CompressionMethod.NotPresent);
            return;
        }

        NetworkSettingsPacket response = new() {
            CompressionThreshold = (ushort)Math.Clamp(server.Properties.CompressionThreshold, 0, ushort.MaxValue),
            CompressionAlgorithm = server.Properties.CompressionMethod.Equals("snappy", StringComparison.OrdinalIgnoreCase)
                ? PacketCompressionAlgorithm.Snappy
                : PacketCompressionAlgorithm.ZLib,
            ClientThrottleEnabled = false,
            ClientThrottleThreshold = 0,
            ClientThrottleScalar = 0f
        };

        server.Network.QueuePacket(connection, response, Protocol.Enums.CompressionMethod.NotPresent);
        connection.NetherNetCompression = true;
    }
}










