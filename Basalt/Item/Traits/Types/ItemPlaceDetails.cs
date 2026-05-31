namespace Basalt.Server.Item.Traits.Types;

using Player = Basalt.Server.Player.Player;
using Basalt.Protocol.Types;


public readonly record struct ItemPlaceDetails(Player Player, int HotBarSlot, BlockPos BlockPosition, int BlockFace, Vec3f Position, Vec3f ClickedPosition);







