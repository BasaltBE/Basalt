using Basalt.Core;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class PlayerAction
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        PlayerActionPacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet.Deserialize(reader);

        if (!server.Players.TryGetValue(connection, out Player? player))
        {
            return;
        }

        player.LastActionFace = packet.BlockFace;
        player.LastActionBlockPosition = packet.BlockPosition;
        player.LastActionResultPosition = packet.ResultPosition;
    }
}
