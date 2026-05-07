using Basalt.Protocol.Types;

namespace Basalt.Entity.Traits.Types;

public readonly record struct EntityFallOnBlockTraitEvent(Vec3f Position, float Distance);
