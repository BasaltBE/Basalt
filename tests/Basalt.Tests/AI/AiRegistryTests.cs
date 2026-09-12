namespace Basalt.Tests;

using Basalt.Core.Entities;
using Basalt.Core.Entities.AI;

public sealed class AiRegistryTests {
    [Fact]
    public void RegistryClassifiesVillagerProfile() {
        EntityType type = EntityType.GetOrCreate(EntityIdentifier.Villager.ToIdentifierString());

        Assert.Equal("villager", AiRegistry.Resolve(type)?.Identifier);
    }

    [Fact]
    public void RegistryClassifiesPassiveProfile() {
        EntityType type = EntityType.GetOrCreate("minecraft:chicken");

        Assert.Equal("passive", AiRegistry.Resolve(type)?.Identifier);
    }

    [Fact]
    public void RegistryDoesNotCreateProfileForItems() {
        EntityType type = EntityType.GetOrCreate(EntityIdentifier.Item.ToIdentifierString());

        Assert.Null(AiRegistry.Resolve(type));
    }
}
