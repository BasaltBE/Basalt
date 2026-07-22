namespace Basalt.Core.Network.Handlers;

using Basalt.Core.Tasks;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Io;
using Basalt.Protocol.Packets;
using Basalt.RakNet;
using BinaryReader = Basalt.Binary.BinaryReader;

public static class Login {
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer) {
        int offset = 0;
        BinaryReader reader = new(packetBuffer, ref offset);
        LoginPacket packet = (LoginPacket)Packet.Deserialize(reader);

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

            server.Network.SendPacket(connection, disconnect, CompressionMethod.NotPresent);
            return;
        }

        server.Scheduler.Schedule(new LoginTask(server, connection, packet));
    }
}
