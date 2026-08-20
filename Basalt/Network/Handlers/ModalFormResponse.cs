namespace Basalt.Core.Network.Handlers;

using System.Text;
using BedrockProtocol.Enums;
using BedrockProtocol.Packets;

public static class ModalFormResponse {
    public static void Handle(Server server, NetworkConnection connection, ModalFormResponsePacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            connection.Disconnect();
            return;
        }

        if (!player.PendingForms.Remove(packet.FormID, out Player.PendingForm? participant)) {
            return;
        }
        var canceled = false;
        if(
            packet.FormCancelReason == ModalFormCancelReason.UserClosed ||
            packet.FormCancelReason == ModalFormCancelReason.UserBusy
        ) canceled = true;

        participant.Complete(
            packet.JSONResponse is null ? null : Encoding.UTF8.GetString(packet.JSONResponse),
            canceled
        );
    }
}
