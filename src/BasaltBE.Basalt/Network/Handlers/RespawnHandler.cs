using BasaltBE.Basalt.Events.Players;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class RespawnHandler {
    public void Handle(NethernetPeer peer, Player? player, RespawnPacket packet) {
        if (player is null) throw new InvalidDataException("Respawn arrived before player login.");
        using Lock.Scope tickSync = player.TickSync.EnterScope();
        if (player.Disposed || !player.Spawned || !player.Dead ||
            packet.State != PlayerRespawnState.ClientReadyToSpawn ||
            player.GetTrait<EntityHealthTrait>()?.Respawn() != true)
            return;

        player.Position.X = player.Dimension.SpawnPosition.X;
        player.Position.Y = player.Dimension.SpawnPosition.Y;
        player.Position.Z = player.Dimension.SpawnPosition.Z;
        player.Rotation.X = 0;
        player.Rotation.Y = 0;
        player.HeadRotation = 0;
        player.SetFlag(BasaltBE.Basalt.Entities.Enums.ActorFlag.Moving, false);
        player.OnRespawn();
        player.OnMove();

        if (player.Network.Server.Events.HasSubscribers<PlayerRespawnEvent>())
            player.Network.Server.Events.Emit(new PlayerRespawnEvent(player));

        player.SendPacket(new RespawnPacket {
            Position = player.GetHeadPosition(),
            State = PlayerRespawnState.ReadyToSpawn,
            RuntimeId = player.RuntimeId
        });
    }
}
