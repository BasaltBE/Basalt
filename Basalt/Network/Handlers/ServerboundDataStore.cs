namespace Basalt.Core.Network.Handlers;

using Basalt.Core.DDUI;
using Basalt.RakNet;
using BedrockProtocol.Packets;

public static class ServerboundDataStore {
    public static void Handle(Server server, NetworkConnection connection, ServerboundDataStorePacket packet) {
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
