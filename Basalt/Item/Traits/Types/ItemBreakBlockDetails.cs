namespace Basalt.Server.Item.Traits.Types;

using Player = Basalt.Server.Player.Player;
using Basalt.Protocol.Types;


public readonly record struct ItemBreakBlockDetails(Player Player, int HotBarSlot, BlockPos BlockPosition, int BlockFace);







