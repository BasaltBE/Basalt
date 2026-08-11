namespace Basalt.Core.Item.Traits.Types;

using BedrockProtocol.Types;
using Player = Basalt.Core.Player.Player;


public readonly record struct ItemPlaceDetails(Player Player, int HotBarSlot, BlockPos BlockPosition, int BlockFace, Vec3 Position, Vec3 ClickedPosition);







