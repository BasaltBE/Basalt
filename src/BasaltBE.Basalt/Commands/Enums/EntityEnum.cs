using System.Collections.Frozen;
using BasaltBE.Basalt.Entities.Types;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class EntityEnum() : IdentifierEnum<EntityType>("EntityType", Values)
{
    private static readonly FrozenDictionary<string, EntityType> Values =
        Index(EntityPalette.Types, static type => type.Identifier);
}
