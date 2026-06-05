namespace Basalt.Core.Events;

using Basalt.Core.Player;

public sealed class PlayerChatSignal : PlayerSignal
{
    public override ServerEvent Event => ServerEvent.PlayerChat;
    public string RawMessage { get; }
    public string Message;
    public bool Cancelled;

    public PlayerChatSignal(Player player, string rawMessage, string message) : base(player)
    {
        RawMessage = rawMessage;
        Message = message;
    }

    public bool Emit()
    {
        return !Cancelled;
    }

    public void Cancel()
    {
        Cancelled = true;
    }
}






