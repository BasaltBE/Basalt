namespace Basalt.Core.Network.Handlers;

using Basalt.Core;

using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Enums;

public static class PlayerAction {
    public static void Handle(Server server, NetworkConnection connection, PlayerActionPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            return;
        }

        if (packet.PlayerRuntimeId != player.RuntimeId) {
            return;
        }

        if (packet.Action == PlayerActionType.Respawn) {
            player.Respawn();
            return;
        }

        player.LastActionFace = packet.Face;
        player.LastActionBlockPosition = packet.BlockPosition;
        player.LastActionResultPosition = packet.ResultPosition;
    }
}
