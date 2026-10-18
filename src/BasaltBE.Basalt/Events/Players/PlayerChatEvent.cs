using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Events.Players;

public sealed class PlayerChatEvent(Player player, string message) : Event {
    public readonly Player Player = player;
    public readonly string Message = message;
}
