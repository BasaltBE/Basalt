namespace Basalt.Core.Network.Handlers;

using System.Text;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Packets;

public static class ModalFormResponse {
    public static void Handle(Server server, NetworkConnection connection, ModalFormResponsePacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            connection.Disconnect();
            return;
        }

        if (player.Dimension is not { } dimension ||
            !dimension.TryEnqueue(player, () => Process(server, connection, player, packet))) {
            return;
        }
    }

    private static void Process(
        Server server,
        NetworkConnection connection,
        Player.Player player,
        ModalFormResponsePacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? current) ||
            !ReferenceEquals(current, player)) {
            return;
        }

        if (!player.PendingForms.Remove(packet.FormId, out Player.PendingForm? participant)) {
            return;
        }
        var canceled = false;
        if(
            packet.FormCancelReason == ModalFormCancelReason.UserClosed ||
            packet.FormCancelReason == ModalFormCancelReason.UserBusy
        ) canceled = true;

        participant.Complete(
            packet.JsonResponse,
            canceled
        );
    }
}
