namespace Basalt.Core.Blocks.Traits.Types;

using Basalt.Core;
using Basalt.BedrockProtocol.Types;

public readonly record struct BlockInteractDetails(Player.Player Player, BlockPos BlockPosition, int BlockFace, Vec3 ClickedPosition);







