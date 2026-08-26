namespace Basalt.Core.Network.Handlers;

using Basalt.Core;

using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Enums;

public static class PlayerAction {
    public static void Handle(Server server, NetworkConnection connection, PlayerActionPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player) ||
            player.Dimension is not { } dimension ||
            !dimension.TryEnqueue(player, () => Process(server, connection, player, packet))) {
            return;
        }
    }

    private static void Process(
        Server server,
        NetworkConnection connection,
        Player.Player player,
        PlayerActionPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? current) ||
            !ReferenceEquals(current, player)) {
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
