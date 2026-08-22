namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Entities.Traits.Attribute;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Packets;

public static class Respawn {
    public static void Handle(Server server, NetworkConnection connection, RespawnPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            return;
        }

        if (packet.PlayerRuntimeId != player.RuntimeId) {
            return;
        }

        if (packet.State != PlayerRespawnState.ClientReadyToSpawn) {
            return;
        }

        if (player.IsAlive && player.GetTrait<EntityHealthTrait>() is { } health &&
            health.CurrentValue <= health.MinimumValue) {
            health.Reset();
            player.Attributes.Send();
        }

        player.Send(new RespawnPacket {
            Position = player.Dimension?.SpawnPosition ?? player.Location,
            State = PlayerRespawnState.ReadyToSpawn,
            PlayerRuntimeId = player.RuntimeId
        });

        if (!player.IsAlive) {
            player.Respawn();
        }
    }
}
