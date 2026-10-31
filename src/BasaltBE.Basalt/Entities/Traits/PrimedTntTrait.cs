using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities.Traits;

[EntityTraitAppliesTo(Identifiers = ["minecraft:tnt"])]
public sealed class PrimedTntTrait(Entity entity) : EntityTrait(entity) {
    const int FuseUpdateInterval = 5;

    public int FuseTicks = 80;

    public override void OnAdd() {
        Entity.MovementUpdateInterval = 10;
        Entity.SetFlag(ActorFlag.Ignited, true);
    }

    public override void Tick() {
        if (Entity.SpawnedDimension is not { } dimension || Entity.Dead)
            return;

        FuseTicks--;
        if (FuseTicks <= 0) {
            dimension.Explode(Entity);
            dimension.RemoveEntity(Entity);
            return;
        }

        if (FuseTicks % FuseUpdateInterval == 0) {
            dimension.Broadcast(new SetActorDataPacket {
                ActorRuntimeId = Entity.RuntimeId,
                ActorData = [CreateFuseTime()],
                Tick = 0
            }, Entity);
        }
    }

    internal ActorDataItem CreateFuseTime() => new() {
        Id = (uint)ActorDataId.FuseTime,
        Type = DataItemType.Int,
        Value = FuseTicks
    };
}
