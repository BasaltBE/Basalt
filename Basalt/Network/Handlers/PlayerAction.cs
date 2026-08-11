namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.RakNet;

using BedrockProtocol.Packets;
using BedrockProtocol.Enums;

public static class PlayerAction {
    public static void Handle(Server server, NetworkConnection connection, PlayerActionPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            return;
        }

        if (packet.PlayerRuntimeID.Value != player.RuntimeId) {
            return;
        }

        if (packet.Action == PlayerActionType.Respawn) {
            player.Respawn();
            return;
        }

        player.LastActionFace = packet.Face;
        player.LastActionBlockPosition = packet.BlockPosition;
        player.LastActionResultPosition = packet.ResultPos;
    }
}
