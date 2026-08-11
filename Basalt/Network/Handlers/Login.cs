namespace Basalt.Core.Network.Handlers;

using Basalt.Protocol.Io;
using Basalt.RakNet;

using BedrockProtocol.Packets;
using BedrockProtocol.Enums;

public static class LoginHandler {
    public static void Handle(Server server, NetworkConnection connection, LoginPacket packet) {
        if (packet.ClientNetworkVersion != Constants.ProtocolVersion) {
            DisconnectFailReason reason = packet.ClientNetworkVersion < Constants.ProtocolVersion
                ? DisconnectFailReason.OutdatedClient
                : DisconnectFailReason.OutdatedServer;

            DisconnectPacket disconnect = new() {
                Reason = reason,
            };

            Logger.Warn($"Session failed due to {reason.ToString()}");
            server.Network.QueuePacket(connection, disconnect, Protocol.Enums.CompressionMethod.NotPresent);
            return;
        }

        server.Scheduler.Schedule(new LoginTask(server, connection, packet));
    }
}
