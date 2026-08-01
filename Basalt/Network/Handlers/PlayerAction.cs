namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.RakNet;


public static class PlayerAction {
    public static void Handle(Server server, NetworkConnection connection, PlayerActionPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            return;
        }

        if (packet.EntityRuntimeId != player.RuntimeId) {
            return;
        }

        if (packet.ActionType == PlayerActionType.Respawn) {
            player.Respawn();
            return;
        }

        player.LastActionFace = packet.BlockFace;
        player.LastActionBlockPosition = packet.BlockPosition;
        player.LastActionResultPosition = packet.ResultPosition;
    }
}
