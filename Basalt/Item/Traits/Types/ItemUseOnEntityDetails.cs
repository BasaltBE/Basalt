using Basalt.Core;
using Basalt.Protocol.Types;

namespace Basalt.Item.Traits.Types;

public readonly record struct ItemUseOnEntityDetails(Player Player, Basalt.Entity.Entity Target, int HotBarSlot, Vec3f Position, Vec3f ClickedPosition);
