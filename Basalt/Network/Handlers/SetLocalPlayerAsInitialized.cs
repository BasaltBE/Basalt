using Basalt.Core;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class SetLocalPlayerAsInitialized
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        SetLocalPlayerAsInitializedPacket packet = new();
        packet.Deserialize(packetBuffer);

        if (!server.Players.TryGetValue(connection, out Player? player))
        {
            Logger.Warn("SetLocalPlayerAsInitialized received for unknown player session.");
            return;
        }

        Logger.Info($"Player {player.Username} has spawned.");
    }
}
