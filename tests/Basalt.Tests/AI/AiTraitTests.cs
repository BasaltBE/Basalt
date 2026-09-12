namespace Basalt.Tests;

using Basalt.Core.Entities;
using Basalt.Core.Entities.AI;
using Basalt.Core.Entities.AI.Memory;
using Basalt.Core.Entities.Traits;
using Basalt.Core.Entities.Traits.Types;

public sealed class AiTraitTests {
    [Fact]
    public void VillagerReceivesAiTrait() {
        Entity villager = new(EntityIdentifier.Villager.ToIdentifierString());

        Assert.NotNull(villager.GetTrait<AiTrait>());
    }

    [Fact]
    public void AiTraitStoresDamageMemory() {
        Entity villager = new(EntityIdentifier.Villager.ToIdentifierString());
        Entity attacker = new(EntityIdentifier.Zombie.ToIdentifierString());
        AiTrait ai = villager.GetTrait<AiTrait>()!;

        ai.OnHurt(new EntityHurtDetails(Basalt.BedrockProtocol.Enums.ActorDamageCause.EntityAttack, attacker));

        Assert.True(ai.Memories.Has(MemoryKeys.HurtBy));
        Assert.True(ai.Memories.Has(MemoryKeys.IsPanicking));
    }
}
