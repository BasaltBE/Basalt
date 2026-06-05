namespace Basalt.Core.Item.Traits.Types;

using Player = Basalt.Core.Player.Player;
using Basalt.Protocol.Types;


public readonly record struct ItemUseOnBlockDetails(Player Player, int HotBarSlot, BlockPos BlockPosition, int BlockFace, Vec3f Position, Vec3f ClickedPosition);







