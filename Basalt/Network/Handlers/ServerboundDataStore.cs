namespace Basalt.Core.Network.Handlers;

using Basalt.Core.DDUI;
using Basalt.Protocol.Packets;
using Basalt.RakNet;

public static class ServerboundDataStore {
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer) {
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        ServerboundDataStorePacket packet = (ServerboundDataStorePacket)Protocol.Io.Packet.Deserialize(reader);

        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            connection.Disconnect();
            return;
        }

        if (!player.Screens.TryGetValue(packet.Update.Property, out DataDrivenScreen? screen)) {
            return;
        }

        screen.Handle(player, packet.Update);
    }
}
