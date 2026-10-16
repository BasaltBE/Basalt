using System.Numerics;
using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Events.Players;

public sealed class PlayerMoveEvent(Player player, Vector3 from, Vector3 to) : Event
{
    public readonly Player Player = player;
    public readonly Vector3 From = from;
    public readonly Vector3 To = to;
}
