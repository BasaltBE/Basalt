namespace Basalt.Core.Item.Traits.Types;

using Basalt.BedrockProtocol.Types;
using Player = Basalt.Core.Player.Player;


public readonly record struct ItemUseOnBlockDetails(Player Player, int HotBarSlot, BlockPos BlockPosition, int BlockFace, BlockPos Position, Vec3 ClickedPosition);