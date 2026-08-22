namespace Basalt.Core.Item.Traits.Types;

using Basalt.BedrockProtocol.Types;
using Player = Basalt.Core.Player.Player;


public readonly record struct ItemUseAttackDetails(Player Player, Basalt.Core.Entities.Entity Target, int HotBarSlot, Vec3 Position, Vec3 ClickedPosition);







