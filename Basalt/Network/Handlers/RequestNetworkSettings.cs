namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Protocol;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Io;
using Basalt.Protocol.Packets;
using Basalt.RakNet;


public static class RequestNetworkSettings {
    public static void Handle(Server server, NetworkConnection connection, RequestNetworkSettingsPacket packet) {
        if (packet.Protocol != Constants.ProtocolVersion) {
            DisconnectReason reason = packet.Protocol < Constants.ProtocolVersion
                ? DisconnectReason.OutdatedClient
                : DisconnectReason.OutdatedServer;

            DisconnectPacket disconnect = new() {
                Reason = reason,
                HideDisconnectionScreen = true,
                Message = "",
                FilteredMessage = ""
            };

            server.Network.QueuePacket(connection, disconnect, CompressionMethod.NotPresent);
            return;
        }

        NetworkSettingsPacket response = new() {
            CompressionThreshold = (ushort)Math.Clamp(server.Properties.CompressionThreshold, 0, ushort.MaxValue),
            CompressionMethod = server.Properties.CompressionMethod.Equals("snappy", StringComparison.OrdinalIgnoreCase)
                ? CompressionMethod.Snappy
                : CompressionMethod.Zlib,
            ClientThrottle = false,
            ClientThrottleThreshold = 0,
            ClientThrottleScalar = 0f
        };

        server.Network.QueuePacket(connection, response, CompressionMethod.NotPresent);
    }
}










