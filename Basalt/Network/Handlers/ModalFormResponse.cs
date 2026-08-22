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
