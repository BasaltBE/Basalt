using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Network;
using Basalt.RakNet;

namespace Basalt.Core;

public sealed class Player : Basalt.Entity.Entity
{
    public readonly string Username;
    public readonly string Xuid;
    public readonly string Uuid;
    internal NetworkConnection? Connection { get; set; }
    internal NetworkHandler? Network { get; set; }
    public PlayerAbilities Abilities { get; } = new();
    public Gamemode Gamemode { get; private set; } = Gamemode.Survival;

    public Player( string username, string xuid, string uuid) : 
        base(EntityIdentifier.Player.ToIdentifierString())
    {
        Username = username;
        Xuid = xuid;
        Uuid = uuid;
        Flags.SetActorFlag(ActorFlag.HasGravity, true);
        Flags.SetActorFlag(ActorFlag.Breathing, true);
    }

    public Gamemode GetGamemode()
    {
        return Gamemode;
    }

    public void SetGamemode(Gamemode gamemode)
    {
        Gamemode = gamemode;
    }

    public void Send(params DataPacket[] packets)
    {
        if (Connection is null || Network is null || packets.Length == 0)
        {
            return;
        }

        Network.SendPackets(Connection, packets);
    }
}
