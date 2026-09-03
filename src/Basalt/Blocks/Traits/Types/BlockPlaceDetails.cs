namespace Basalt.Core.Blocks.Traits.Types;

using Basalt.Core;
using Basalt.BedrockProtocol.Types;

public readonly record struct BlockPlaceDetails(Player.Player Player, BlockPos BlockPosition, int BlockFace, BlockPos ClickedPosition);







