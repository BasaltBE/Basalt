namespace Basalt.Core.Entities.Traits;

using Basalt.BedrockProtocol.NBT;
using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Traits;
using Basalt.Core.Worlds;

public sealed class EntityAgeableTrait : EntityTrait {
    public new static string Identifier => "ageable";
    public new static readonly EntityIdentifier[] Types = [
        EntityIdentifier.Cow,
        EntityIdentifier.Sheep,
        EntityIdentifier.Pig,
        EntityIdentifier.Chicken,
        EntityIdentifier.Rabbit,
        EntityIdentifier.Horse,
        EntityIdentifier.Donkey,
        EntityIdentifier.Mule,
        EntityIdentifier.Llama,
        EntityIdentifier.TraderLlama,
        EntityIdentifier.Goat,
        EntityIdentifier.Turtle,
        EntityIdentifier.Wolf,
        EntityIdentifier.Cat,
        EntityIdentifier.Fox,
        EntityIdentifier.Panda,
        EntityIdentifier.PolarBear,
        EntityIdentifier.Parrot,
        EntityIdentifier.Ocelot,
        EntityIdentifier.Bee,
        EntityIdentifier.Frog,
        EntityIdentifier.Armadillo,
        EntityIdentifier.Sniffer,
        EntityIdentifier.Dolphin,
        EntityIdentifier.Axolotl,
        EntityIdentifier.Strider,
        EntityIdentifier.Camel
    ];

    public int AgeTicks { get; private set; }

    public EntityAgeableTrait(Entity entity) : base(entity) {
    }

    public override void OnAdd() {
        if (Entity.Flags.GetActorFlag(ActorFlag.Baby) && AgeTicks == 0) {
            AgeTicks = -24000;
        }
    }

    public override void OnSpawn(EntitySpawnOptions details) {
        if (details.InitialSpawn && Entity.Flags.GetActorFlag(ActorFlag.Baby) && AgeTicks == 0) {
            AgeTicks = -24000;
        }
    }

    public override void OnTick(TraitOnTickDetails details) {
        if (!Entity.Flags.GetActorFlag(ActorFlag.Baby)) {
            return;
        }

        if (AgeTicks == 0) {
            AgeTicks = -24000;
        }

        AgeTicks = Math.Min(0, AgeTicks + (int)Math.Min(details.DeltaTick, int.MaxValue));
        if (AgeTicks == 0) {
            Entity.Flags.SetActorFlag(ActorFlag.Baby, false);
        }
    }

    public void AgeUp(int ticks) {
        if (ticks <= 0 || !Entity.Flags.GetActorFlag(ActorFlag.Baby)) {
            return;
        }

        if (AgeTicks == 0) {
            AgeTicks = -24000;
        }

        AgeTicks = Math.Min(0, AgeTicks + ticks);
        if (AgeTicks == 0) {
            Entity.Flags.SetActorFlag(ActorFlag.Baby, false);
        }
    }

    public override void OnRead(CompoundTag entityTag, CompoundTag traitTag) {
        AgeTicks = traitTag.Get<IntTag>("age_ticks")?.Value ?? 0;
    }

    public override void OnWrite(CompoundTag entityTag, CompoundTag traitTag) {
        traitTag.Set("age_ticks", new IntTag { Value = AgeTicks });
    }

    public override EntityTrait Clone(Entity entity) => new EntityAgeableTrait(entity) {
        AgeTicks = AgeTicks
    };
}
