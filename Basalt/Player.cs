using Basalt.Protocol.Enums;

namespace Basalt.Core;

public sealed class Player : Basalt.Entity.Entity
{
    public readonly string Username;
    public readonly string Xuid;
    public readonly string Uuid;
    public PlayerAbilities Abilities { get; } = new();
    public Gamemode Gamemode { get; private set; } = Gamemode.Survival;

    public Player( string username, string xuid, string uuid) : 
        base(EntityIdentifier.Player.ToIdentifierString())
    {
        Username = username;
        Xuid = xuid;
        Uuid = uuid;
    }

    public Gamemode GetGamemode()
    {
        return Gamemode;
    }

    public void SetGamemode(Gamemode gamemode)
    {
        Gamemode = gamemode;
    }
}
