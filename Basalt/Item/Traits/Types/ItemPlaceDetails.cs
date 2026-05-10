using Basalt.Core;
using Basalt.Protocol.Types;

namespace Basalt.Item.Traits.Types;

public readonly record struct ItemPlaceDetails(Player Player, int HotBarSlot, BlockPos BlockPosition, int BlockFace, Vec3f Position, Vec3f ClickedPosition);
