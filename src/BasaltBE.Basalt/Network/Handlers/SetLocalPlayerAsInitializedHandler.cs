using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Events.Players;
using BasaltBE.Basalt.Profiling;
using BasaltBE.Logging;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class SetLocalPlayerAsInitializedHandler {
    public void Handle(NethernetPeer peer, Player? player, SetLocalPlayerAsInitializedPacket packet) {
        using ProfilerZone zone = Profiler.BeginZone("Network.SetLocalPlayerAsInitialized");
        if (player is null)
            throw new InvalidDataException("Player initialization arrived before player login.");
        if (packet.RuntimeId != player.RuntimeId)
            throw new InvalidDataException("Player initialization runtime ID does not match the logged in player.");

        using Lock.Scope tickSync = player.TickSync.EnterScope();
        if (player.Spawned || player.Disposed)
            return;
        player.Spawned = true;
        player.OnSpawn();
        PlayerSpawnEvent spawned = new(player);
        player.Network.Server.Events.Emit(spawned);
        Logger.Scoped("Network").Info($"Player {player.Username} Spawned in.");
    }
}
