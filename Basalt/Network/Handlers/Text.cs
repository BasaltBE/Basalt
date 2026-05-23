using Basalt.Core;
using Basalt.Entity.Traits.PlayerTraits;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

namespace Basalt.Network.Handlers;

public static class Text
{
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer)
    {
        TextPacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet = (TextPacket)Protocol.Io.Packet.Deserialize(reader);

        if (!server.Players.TryGetValue(connection, out Player? sender))
        {
            Logger.Warn("Text received for unknown player session.");
            return;
        }

        foreach (Player player in server.Players.Values)
            player.SendMessage($"§r<{sender.Username}> {packet.Variant.Message}");
    }
}

