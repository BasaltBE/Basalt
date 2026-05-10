using Basalt.Core;
using Basalt.Protocol.Types;

namespace Basalt.Item.Traits.Types;

public readonly record struct ItemBreakBlockDetails(Player Player, int HotBarSlot, BlockPos BlockPosition, int BlockFace);
