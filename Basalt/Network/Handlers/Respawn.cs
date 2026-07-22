namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Events;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

public static class Respawn {
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer) {
        RespawnPacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet = (RespawnPacket)Protocol.Io.Packet.Deserialize(reader);

        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            return;
        }

        if (packet.EntityRuntimeId != player.RuntimeId) {
            return;
        }

        if (packet.State != RespawnState.ClientReadyToSpawn) {
            return;
        }

        PlayerRespawnSignal signal = new(player);
        server.Emit(signal);
        if (!signal.Emit()) {
            return;
        }

        player.Send(new RespawnPacket {
            Position = player.Location,
            State = RespawnState.ReadyToSpawn,
            EntityRuntimeId = player.RuntimeId
        });

        player.Respawn();
    }
}
