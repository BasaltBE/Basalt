namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Protocol.Packets;
using Basalt.RakNet;


public static class PlayerAction {
    public static void Handle(Server server, NetworkConnection connection, PlayerActionPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            return;
        }

        player.LastActionFace = packet.BlockFace;
        player.LastActionBlockPosition = packet.BlockPosition;
        player.LastActionResultPosition = packet.ResultPosition;
    }
}










