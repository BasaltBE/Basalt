namespace Basalt.Core.Network.Handlers;

using Basalt.Core.Tasks;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Io;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

public static class Login {
    public static void Handle(Server server, NetworkConnection connection, LoginPacket packet) {
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

        server.Scheduler.Schedule(new LoginTask(server, connection, packet));
    }
}
