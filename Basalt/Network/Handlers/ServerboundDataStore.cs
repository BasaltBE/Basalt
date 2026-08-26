namespace Basalt.Core.Network.Handlers;

using Basalt.Core.DDUI;
using Basalt.BedrockProtocol.Packets;

public static class ServerboundDataStore {
    public static void Handle(Server server, NetworkConnection connection, ServerboundDataStorePacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? player)) {
            connection.Disconnect();
            return;
        }

        if (player.Dimension is not { } dimension ||
            !dimension.TryEnqueue(player, () => Process(server, connection, player, packet))) {
            return;
        }
    }

    private static void Process(
        Server server,
        NetworkConnection connection,
        Player.Player player,
        ServerboundDataStorePacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? current) ||
            !ReferenceEquals(current, player)) {
            return;
        }

        if (!player.Screens.TryGetValue(packet.Update.Property, out DataDrivenScreen? screen)) {
            return;
        }

        screen.Handle(player, packet.Update);
    }
}
