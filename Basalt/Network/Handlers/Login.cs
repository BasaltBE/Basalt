namespace Basalt.Core.Network.Handlers;


using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Enums;

public static class LoginHandler {
    public static void Handle(Server server, NetworkConnection connection, LoginPacket packet) {
        if (packet.ClientNetworkVersion != Constants.ProtocolVersion) {
            PlayStatus status = packet.ClientNetworkVersion < Constants.ProtocolVersion
                ? PlayStatus.LoginFailedClientOld
                : PlayStatus.LoginFailedServerOld;

            PlayStatusPacket playStatus = new() {
                Status = status,
            };

            Logger.Warn($"Session(!) failed due to {status} {packet.ClientNetworkVersion} != {Constants.ProtocolVersion}");
            server.Network.QueuePacket(connection, playStatus, CompressionMethod.NotPresent);
            return;
        }

        server.Scheduler.Schedule(new LoginTask(server, connection, packet));
    }
}
