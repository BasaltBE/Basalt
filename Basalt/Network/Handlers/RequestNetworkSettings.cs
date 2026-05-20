using Basalt.Core;
using Basalt.Protocol;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class RequestNetworkSettings
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        RequestNetworkSettingsPacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet.Deserialize(reader);

        if (packet.ProtocolVersion != ProtocolInfo.ProtocolVersion)
        {
            DisconnectReason reason = packet.ProtocolVersion < ProtocolInfo.ProtocolVersion
                ? DisconnectReason.OutdatedClient
                : DisconnectReason.OutdatedServer;

            DisconnectPacket disconnect = new()
            {
                Reason = reason,
                HideDisconnectionScreen = true,
                Message = "",
                FilteredMessage = ""
            };

            server.Network.SendPacket(connection, disconnect, CompressionMethod.NotPresent);
            return;
        }

        NetworkSettingsPacket response = new(
            compressionThreshold: server.Options.CompressionThreshold,
            compressionMethod: server.Options.CompressionMethod,
            clientThrottle: false,
            clientThrottleThreshold: 0,
            clientThrottleScalar: 0f
        );

        server.Network.SendPacket(connection, response, CompressionMethod.NotPresent);
    }
}
