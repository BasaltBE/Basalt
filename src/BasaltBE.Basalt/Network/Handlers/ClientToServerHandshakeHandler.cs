using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Profiling;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class ClientToServerHandshakeHandler {
    public void Handle(NethernetPeer peer, Player? player, ClientToServerHandshakePacket packet) {
        using ProfilerZone zone = Profiler.BeginZone("Network.ClientToServerHandshake");
        if (player is null || player.Encryption is null || player.Encryption.Enabled)
            throw new InvalidDataException("Bedrock encryption handshake arrived in the wrong connection state.");
        player.Encryption.Enabled = true;
    }
}
