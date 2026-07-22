namespace Basalt.Core.Network.Handlers;

using Basalt.Protocol.Packets;
using Basalt.RakNet;

public static class ModalFormResponse {
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer) {
        ModalFormResponsePacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet = (ModalFormResponsePacket)Protocol.Io.Packet.Deserialize(reader);

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
