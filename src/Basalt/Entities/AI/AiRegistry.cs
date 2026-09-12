namespace Basalt.Core.Entities.AI;

using Basalt.Core.Entities;

public static class AiRegistry {
    private static readonly AiProfile Passive = new("passive", []);
    private static readonly AiProfile Hostile = new("hostile", []);
    private static readonly AiProfile Villager = new("villager", []);

    public static AiProfile? Resolve(EntityType type) {
        ArgumentNullException.ThrowIfNull(type);

        if (type.Identifier is "minecraft:villager" or "minecraft:villager_v2") {
            return Villager;
        }

        if (type.Components.Contains("minecraft:behavior.nearest_attackable_target") ||
            type.Components.Contains("minecraft:behavior.melee_attack") ||
            type.Components.Contains("minecraft:behavior.ranged_attack")) {
            return Hostile;
        }

        return type.Components.Contains("minecraft:behavior.random_stroll")
            ? Passive
            : null;
    }
}
