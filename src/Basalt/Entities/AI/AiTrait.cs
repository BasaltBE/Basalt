namespace Basalt.Core.Entities.AI;

using Basalt.Core.Entities.AI.Goal;
using Basalt.Core.Entities.AI.Memory;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Traits;
using Basalt.Core.Worlds;

public sealed class AiTrait : EntityTrait {
    public new static string Identifier => "ai";
    public new static readonly string[] Components = [
        "minecraft:behavior.random_stroll",
        "minecraft:behavior.panic",
        "minecraft:behavior.nearest_attackable_target"
    ];

    public MemoryStore Memories { get; } = new();
    public GoalSelector Goals { get; } = new();
    public AiProfile? Profile { get; private set; }

    public AiTrait(Entity entity) : base(entity) {
    }

    public override void OnAdd() {
        Profile = AiRegistry.Resolve(Entity.Type);
        if (Profile is null) {
            return;
        }

        for (int i = 0; i < Profile.GoalFactories.Count; i++) {
            Goals.Add(Profile.GoalFactories[i](Entity));
        }
    }

    public override void OnTick(TraitOnTickDetails details) {
        if (!Entity.IsAlive || Entity.Dimension is null) {
            return;
        }

        Memories.Expire(details.CurrentTick);
        Goals.Tick(new AiContext(Entity, Memories, Goals, details.CurrentTick));
    }

    public override void OnHurt(EntityHurtDetails details) {
        if (details.Damager is not null) {
            ulong? expiresAt = Entity.Dimension?.World is Tickable world
                ? world.TickValue + 40
                : null;
            Memories.Set(MemoryKeys.HurtBy, details.Damager, expiresAt);
        }

        ulong? panicExpiresAt = Entity.Dimension?.World is Tickable panicWorld
            ? panicWorld.TickValue + 100
            : null;
        Memories.Set(MemoryKeys.IsPanicking, true, panicExpiresAt);
    }

    public override void OnDespawn(EntityDespawnOptions details) {
        Memories.Remove(MemoryKeys.HurtBy);
        Memories.Remove(MemoryKeys.IsPanicking);
    }

    public override EntityTrait Clone(Entity entity) => new AiTrait(entity);
}
