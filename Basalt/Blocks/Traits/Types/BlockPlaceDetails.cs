namespace Basalt.Core.Blocks.Traits.Types;

using Basalt.Core;
using Basalt.Protocol.Types;


public readonly record struct BlockPlaceDetails(Player.Player Player, BlockPos BlockPosition, int BlockFace, Vec3f ClickedPosition);







