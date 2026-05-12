using Basalt.Core;
using Basalt.Protocol.Types;

namespace Basalt.Block.Traits.Types;

public readonly record struct BlockPlaceDetails(Player Player, BlockPos BlockPosition, int BlockFace, Vec3f ClickedPosition);
