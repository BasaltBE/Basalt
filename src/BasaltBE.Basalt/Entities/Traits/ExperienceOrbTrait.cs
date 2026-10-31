using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities.Traits;

[EntityTraitAppliesTo(Identifiers = ["minecraft:xp_orb"])]
public sealed class ExperienceOrbTrait(Entity entity) : EntityTrait(entity) {
    private const ulong LifetimeTicks = 6000;
    private const ulong PickupDelayTicks = 10;
    private const ulong MergeIntervalTicks = 40;
    private const float AttractionRadiusSquared = 64;
    private const float PickupRadiusSquared = 2.25f;
    private const float MergeRadiusSquared = 1;

    internal int ExperienceValue = 1;

    internal ActorDataItem CreateExperienceValue() => new() {
        Id = (uint)ActorDataId.Value,
        Type = DataItemType.Int,
        Value = ExperienceValue
    };

    public override void OnAdd() {
        EntityMovementTrait movement = Entity.GetTrait<EntityMovementTrait>()
            ?? throw new InvalidOperationException("Experience orbs require an entity movement trait.");
        movement.Gravity = 0.03f;
        movement.Drag = 0.02f;
        movement.TerminalVelocity = -0.4f;
        movement.VerticalDragBeforeGravity = true;
        movement.Motion.X = (Random.Shared.NextSingle() - 0.5f) * 0.1f;
        movement.Motion.Y = 0.1f + Random.Shared.NextSingle() * 0.1f;
        movement.Motion.Z = (Random.Shared.NextSingle() - 0.5f) * 0.1f;
    }

    public override void Tick() {
        if (Entity.Dead || Entity.SpawnedDimension is not { } dimension)
            return;
        if (Entity.SimulationAge >= LifetimeTicks) {
            dimension.Broadcast(new RemoveActorPacket { ActorId = Entity.UniqueId }, Entity);
            Entity.Despawn();
            return;
        }

        Player? nearest = null;
        float nearestDistance = AttractionRadiusSquared;
        foreach (Player player in dimension.GetPlayers()) {
            if (player.Disposed || !player.Spawned || player.Dead || player.SpawnedDimension != dimension)
                continue;

            float dx = player.Position.X - Entity.Position.X;
            float dy = player.Position.Y + 1 - Entity.Position.Y;
            float dz = player.Position.Z - Entity.Position.Z;
            float distance = dx * dx + dy * dy + dz * dz;
            if (distance < nearestDistance) {
                nearestDistance = distance;
                nearest = player;
            }
        }

        if (nearest is not null) {
            EntityMovementTrait movement = Entity.GetTrait<EntityMovementTrait>()!;
            float targetX = nearest.Position.X - Entity.Position.X;
            float targetY = nearest.Position.Y + 1 - Entity.Position.Y;
            float targetZ = nearest.Position.Z - Entity.Position.Z;
            if (nearestDistance > 0.0001f) {
                float distance = MathF.Sqrt(nearestDistance);
                float acceleration = 0.010f * (1 - distance / 8);
                float inverseLength = 1 / distance;
                movement.Motion.X += targetX * inverseLength * acceleration;
                movement.Motion.Y += targetY * inverseLength * acceleration;
                movement.Motion.Z += targetZ * inverseLength * acceleration;
            }

            if (Entity.SimulationAge >= PickupDelayTicks && nearestDistance <= PickupRadiusSquared &&
                nearest.GetTrait<PlayerExperienceTrait>() is { } experience) {
                experience.Collect(ExperienceValue);
                dimension.Broadcast(new TakeItemActorPacket {
                    ItemRuntimeId = Entity.RuntimeId,
                    ActorRuntimeId = nearest.RuntimeId
                }, Entity);
                dimension.Broadcast(new LevelSoundEventPacket {
                    SoundEvent = "random.orb",
                    Position = Entity.Position,
                    ActorUniqueId = unchecked((long)Entity.RuntimeId)
                }, Entity);
                dimension.Broadcast(new RemoveActorPacket { ActorId = Entity.UniqueId }, Entity);
                Entity.Despawn();
                return;
            }
        }

        if (Entity.SimulationAge % MergeIntervalTicks != 0)
            return;

        int chunkX = (int)MathF.Floor(Entity.Position.X) >> 4;
        int chunkZ = (int)MathF.Floor(Entity.Position.Z) >> 4;
        for (int z = chunkZ - 1; z <= chunkZ + 1; z++) {
            for (int x = chunkX - 1; x <= chunkX + 1; x++) {
                foreach (Entity other in dimension.EntityIndex.GetChunkEntities(x, z)) {
                    if (other.RuntimeId <= Entity.RuntimeId || other.Dead ||
                        other.SpawnedDimension != dimension || other.SimulationAge < MergeIntervalTicks ||
                        other.GetTrait<ExperienceOrbTrait>() is not { } otherOrb)
                        continue;

                    float dx = other.Position.X - Entity.Position.X;
                    float dy = other.Position.Y - Entity.Position.Y;
                    float dz = other.Position.Z - Entity.Position.Z;
                    if (dx * dx + dy * dy + dz * dz > MergeRadiusSquared)
                        continue;

                    ExperienceValue = (int)Math.Min(int.MaxValue, (long)ExperienceValue + otherOrb.ExperienceValue);
                    dimension.Broadcast(new SetActorDataPacket {
                        ActorRuntimeId = Entity.RuntimeId,
                        ActorData = [CreateExperienceValue()],
                        Tick = 0
                    }, Entity);
                    dimension.Broadcast(new RemoveActorPacket { ActorId = other.UniqueId }, other);
                    other.Despawn();
                }
            }
        }
    }
}
