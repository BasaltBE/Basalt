namespace Basalt.Core.Network.Handlers;

using System.Buffers.Binary;
using Basalt.Core;
using Basalt.Core.Player;

using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;

public static class PlayerSkin {
    public static void Handle(Server server, NetworkConnection connection, PlayerSkinPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player? player) ||
            player.Dimension is not { } dimension ||
            !dimension.TryEnqueue(player, () => Process(server, connection, player, packet))) {
            return;
        }
    }

    private static void Process(
        Server server,
        NetworkConnection connection,
        Player player,
        PlayerSkinPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player? current) ||
            !ReferenceEquals(current, player)) {
            return;
        }

        if (packet.SerializedSkin.FullId == player.LastRequestedFullSkinId) {
            return;
        }

        player.LastRequestedFullSkinId = packet.SerializedSkin.FullId;
        
        player.Skin = packet.SerializedSkin;

        PlayerSkinPacket skinPacket = new() {
            Uuid = FromGuid(player.Uuid),
            SerializedSkin = packet.SerializedSkin,
            LocalizedNewSkinName = string.Empty,
            LocalizedOldSkinName = string.Empty,
        };
        server.Broadcast(skinPacket);
    }

    private static Uuid FromGuid(Guid guid) {
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes, bigEndian: true, out _);
        return new Uuid {
            MostSignificantBits = BinaryPrimitives.ReadUInt64BigEndian(bytes[..8]),
            LeastSignificantBits = BinaryPrimitives.ReadUInt64BigEndian(bytes[8..])
        };
    }
}
