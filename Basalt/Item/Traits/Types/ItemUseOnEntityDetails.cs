namespace Basalt.Server.Item.Traits.Types;

using Player = Basalt.Server.Player.Player;
using Basalt.Protocol.Types;


public readonly record struct ItemUseOnEntityDetails(Player Player, Basalt.Server.Entity.Entity Target, int HotBarSlot, Vec3f Position, Vec3f ClickedPosition);







