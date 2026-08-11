namespace Basalt.Core.Item.Traits.Types;

using BedrockProtocol.Types;
using Player = Basalt.Core.Player.Player;


public readonly record struct ItemBreakBlockDetails(Player Player, int HotBarSlot, BlockPos BlockPosition, int BlockFace);







