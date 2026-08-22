namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Worlds.Dimensions;

using Basalt.BedrockProtocol.Packets;

public static class Animate {
    public static void Handle(Server server, NetworkConnection connection, AnimatePacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player) || player.Dimension is null) {
            return;
        }

        packet.TargetActorRuntimeId = player.RuntimeId;

        player.Dimension.Broadcast(packet, new BroadcastOptions {
            Center = player.Position,
            Except = [player]
        });
    }
}
