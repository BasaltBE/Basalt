using System.Text;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Events.Players;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;

namespace BasaltBE.Basalt.Network.Handlers;

internal sealed class TextHandler {
    private const int MaximumMessageLength = 512;

    public void Handle(NethernetPeer peer, Player? player, TextPacket packet) {
        if (player is null)
            return;
        if (player.Disposed || !player.Spawned)
            return;
        if (packet.Type != TextMessageType.Chat || packet.Localize ||
            string.IsNullOrWhiteSpace(packet.Message) ||
            packet.Message.Length > MaximumMessageLength ||
            Encoding.UTF8.GetByteCount(packet.Message) > MaximumMessageLength) {
            return;
        }

        foreach (char character in packet.Message) {
            if (char.IsControl(character))
                return;
        }

        PlayerChatEvent chat = new(player, packet.Message);
        player.Network.Server.Events.Emit(chat);
        if (chat.IsCanceled() || player.Disposed)
            return;

        player.Network.Server.Broadcast(new TextPacket {
            Type = TextMessageType.Chat,
            SourceName = player.Username,
            Xuid = player.Xuid,
            Message = packet.Message
        });
    }
}
