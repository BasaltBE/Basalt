namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Events;
using Basalt.Protocol.Packets;
using Basalt.RakNet;


public static class Text {
    public static void Handle(Server server, NetworkConnection connection, ReadOnlySpan<byte> packetBuffer) {
        TextPacket packet = new();
        int offset = 0;
        Binary.BinaryReader reader = new(packetBuffer, ref offset);
        packet = (TextPacket)Protocol.Io.Packet.Deserialize(reader);

        if (!server.Players.TryGetValue(connection, out Player.Player? sender)) {
            Logger.Warn("Text received for unknown player session.");
            return;
        }

        string rawMessage = packet.Variant.Message;
        string message = $"<{sender.Username}> {rawMessage}";
        PlayerChatSignal signal = new(sender, rawMessage, message);
        server.Emit(signal);
        if (!signal.Emit()) {
            return;
        }

        foreach (Player.Player player in server.Players.Values) {
            player.SendMessage(signal.Message);
        }
    }
}









