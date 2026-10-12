using BasaltBE.Basalt.Entities;
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

        player.Spawned = true;
        Logger.Scoped("Network").Info($"Player {player.Username} spawned in.");
    }
}
