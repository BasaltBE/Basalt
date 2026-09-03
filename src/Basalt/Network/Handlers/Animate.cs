namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Worlds.Dimensions;

using Basalt.BedrockProtocol.Packets;

public static class Animate {
    public static void Handle(Server server, NetworkConnection connection, AnimatePacket packet) {
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
        AnimatePacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? current) ||
            !ReferenceEquals(current, player) ||
            player.Dimension is not { } dimension) {
            return;
        }

        packet.TargetActorRuntimeId = player.RuntimeId;

        dimension.Broadcast(packet, new BroadcastOptions {
            Center = player.Position,
            Except = [player]
        });
    }
}
