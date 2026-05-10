using Basalt.Core;
using Basalt.Protocol.Types;

namespace Basalt.Item.Traits.Types;

public readonly record struct ItemUseOnAirDetails(Player Player, int HotBarSlot, Vec3f Position);
