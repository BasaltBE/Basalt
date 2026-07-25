namespace Basalt.Core.Network.Handlers;

using Basalt.Protocol.Packets;
using Basalt.RakNet;

public static class ModalFormResponse {
    public static void Handle(Server server, NetworkConnection connection, ModalFormResponsePacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            connection.Disconnect();
            return;
        }

        if (!player.PendingForms.Remove(packet.FormId, out Player.PendingForm? participant)) {
            return;
        }

        participant.Complete(packet.Data, packet.Canceled);
    }
}
