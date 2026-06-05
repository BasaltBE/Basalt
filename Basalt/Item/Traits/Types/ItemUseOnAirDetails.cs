namespace Basalt.Server.Item.Traits.Types;

using Player = Basalt.Server.Player.Player;
using Basalt.Protocol.Types;


public readonly record struct ItemUseOnAirDetails(Player Player, int HotBarSlot, Vec3f Position);







