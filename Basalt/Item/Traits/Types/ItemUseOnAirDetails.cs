namespace Basalt.Core.Item.Traits.Types;

using BedrockProtocol.Types;
using Player = Basalt.Core.Player.Player;


public readonly record struct ItemUseOnAirDetails(Player Player, int HotBarSlot, BlockPos Position);







