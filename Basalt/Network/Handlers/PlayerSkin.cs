namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Player;

using BedrockProtocol.Packets;
using BedrockProtocol.Types;

public static class PlayerSkin {
    public static void Handle(Server server, NetworkConnection connection, PlayerSkinPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player? player)) {
            return;
        }

        if (packet.SerializedSkin.FullID == player.LastRequestedFullSkinId) {
            return;
        }

        player.LastRequestedFullSkinId = packet.SerializedSkin.FullID;
        
        player.Skin = packet.SerializedSkin;

        PlayerSkinPacket skinPacket = new() {
            UUID = player.GetUUID(),
            SerializedSkin = packet.SerializedSkin,
            LocalizedNewSkinName = string.Empty,
            LocalizedOldSkinName = string.Empty,
        };
        server.Broadcast(skinPacket);
    }
}
