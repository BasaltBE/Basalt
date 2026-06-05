namespace Basalt.Core.Item.Traits.Types;

using Player = Basalt.Core.Player.Player;
using Basalt.Protocol.Types;


public readonly record struct ItemUseOnAirDetails(Player Player, int HotBarSlot, Vec3f Position);







