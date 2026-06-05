namespace Basalt.Core.Item.Traits.Types;

using Player = Basalt.Core.Player.Player;
using Basalt.Protocol.Types;


public readonly record struct ItemUseAttackDetails(Player Player, Basalt.Core.Entity.Entity Target, int HotBarSlot, Vec3f Position, Vec3f ClickedPosition);







