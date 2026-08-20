namespace Basalt.Core.Network.Handlers;

using Basalt.Core;
using Basalt.Core.Events;

using BedrockProtocol.Packets;
using BedrockProtocol.Types;

public static class Text {
    public static void Handle(Server server, NetworkConnection connection, TextPacket packet) {
        if (!server.Players.TryGetValue(connection, out Player.Player? sender)) {
            Logger.Warn("Text received for unknown player session.");
            return;
        }

        string? rawMessage = packet.Body switch {
            MessageOnly body => body.Message,
            AuthorAndMessage body => body.Message,
            _ => null
        };
        if(rawMessage is null) return;

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









