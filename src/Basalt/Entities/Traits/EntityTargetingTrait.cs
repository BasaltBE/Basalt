namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Entities.Behaviors;
using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Components;
using Basalt.Core.Entities;
using Basalt.Core.Pathfinding;
using Basalt.Core.Traits;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Player;
using Basalt.BedrockProtocol.Enums;
using Basalt.BedrockProtocol.Types;
using System.Text.Json;

public sealed class EntityTargetingTrait : EntityTrait {
    public new static string Identifier => "targeting";
    public new static readonly string[] Components = [
        "minecraft:behavior.nearest_attackable_target",
        "minecraft:attack"
    ];

    private const ulong ReselectInterval = 20;
    private Basalt.Core.Pathfinding.Path? _path;
    private int _pathIndex;
    private int _pathRequestId;
    private ulong _nextReselect;
    private ulong _nextPathRequest;
    private bool _pathPending;
    private bool _pathStale;
    private Entity? _target;
    private float _pathTargetX;
    private float _pathTargetY;
    private float _pathTargetZ;
    private bool _fallbackTargeting;
    private float _movementSpeed;

    public Entity? Target => _target;

    public EntityTargetingTrait(Entity entity) : base(entity) {
    }

    public override void OnAdd() {
        _fallbackTargeting = Entity.Type.NearestAttackableTarget is null &&
            Entity.Type.Components.Contains("minecraft:attack");
    }

    public override void OnTick(TraitOnTickDetails details) {
        if (!Entity.IsAlive || Entity.Dimension is not Dimension dimension ||
            Entity.GetTrait<EntityMovementTrait>() is not { Grounded: true } movement ||
            movement.InLava || Entity.IsInWater) {
            return;
        }

        _movementSpeed = movement.AiMovementSpeed;

        NearestAttackableTargetBehavior? behavior = Entity.Type.NearestAttackableTarget;
        if (behavior is null && !_fallbackTargeting) {
            return;
        }

        if (details.CurrentTick >= _nextReselect || _target is null || !_target.IsAlive) {
            _nextReselect = details.CurrentTick + ReselectInterval;
            Entity? next = behavior is null
                ? FindFallbackTarget(dimension)
                : FindTarget(dimension, behavior);
            if (!ReferenceEquals(next, _target)) {
                _target = next;
                _path = null;
                _pathIndex = 0;
                _pathStale = false;
                _pathRequestId++;
            }
        }

        if (_target is null || _target.Dimension != dimension) {
            Stop();
            return;
        }

        Vec3 targetPosition = GetFeetPosition(_target);
        float targetDeltaX = targetPosition.X - _pathTargetX;
        float targetDeltaY = targetPosition.Y - _pathTargetY;
        float targetDeltaZ = targetPosition.Z - _pathTargetZ;
        if (_path is not null &&
            targetDeltaX * targetDeltaX + targetDeltaY * targetDeltaY + targetDeltaZ * targetDeltaZ > 2.25f) {
            _pathStale = true;
            _nextPathRequest = details.CurrentTick;
        }

        if (_path is null || _pathIndex >= _path.Nodes.Count) {
            if (details.CurrentTick >= _nextPathRequest) {
                RequestPath(dimension, details.CurrentTick);
            }

            if (_path is null || _pathIndex >= _path.Nodes.Count) {
                Stop();
            }
            LookAtTarget(_target);
            return;
        }

        if (_pathStale && details.CurrentTick >= _nextPathRequest) {
            RequestPath(dimension, details.CurrentTick);
        }

        FollowPath();
        LookAtTarget(_target);
    }

    public override void OnDespawn(EntityDespawnOptions details) {
        _target = null;
        _path = null;
        _pathPending = false;
        _pathStale = false;
        _pathRequestId++;
    }

    public override EntityTrait Clone(Entity entity) {
        return new EntityTargetingTrait(entity);
    }

    private Entity? FindTarget(Dimension dimension, NearestAttackableTargetBehavior behavior) {
        float radius = behavior.WithinRadius ?? 32f;
        Entity? nearest = null;
        float nearestDistance = float.MaxValue;
        List<Entity> candidates = [];
        GatherEntities(dimension, Entity.Position, radius, candidates);

        foreach (Entity candidate in candidates) {
            if (ReferenceEquals(candidate, Entity) || !candidate.IsAlive || candidate.PendingDespawn) {
                continue;
            }

            if (IsCreativeOrSpectator(candidate)) {
                continue;
            }

            float distance = DistanceSquared(Entity.Position, candidate.Position);
            if (distance > radius * radius || distance >= nearestDistance ||
                !MatchesEntry(dimension, candidate, Entity.Position, behavior.EntityTypes, behavior.MustSee)) {
                continue;
            }

            nearest = candidate;
            nearestDistance = distance;
        }

        return nearest;
    }

    private Entity? FindFallbackTarget(Dimension dimension) {
        Entity? nearest = null;
        float nearestDistance = 32f * 32f;
        List<Entity> candidates = [];
        GatherEntities(dimension, Entity.Position, 32f, candidates);

        foreach (Entity candidate in candidates) {
            if (ReferenceEquals(candidate, Entity) || !candidate.IsAlive || candidate.PendingDespawn ||
                IsCreativeOrSpectator(candidate) || !IsFallbackTarget(candidate)) {
                continue;
            }

            float distance = DistanceSquared(Entity.Position, candidate.Position);
            if (distance >= nearestDistance) {
                continue;
            }

            nearest = candidate;
            nearestDistance = distance;
        }

        return nearest;
    }

    private bool IsFallbackTarget(Entity candidate) {
        if (candidate.IsPlayer()) {
            return true;
        }

        EntityIdentifier entityIdentifier = EntityIdentifierExtensions.FromString(Entity.Identifier);
        if (entityIdentifier is not EntityIdentifier.Zombie and not EntityIdentifier.ZombieVillager) {
            return false;
        }

        EntityIdentifier targetIdentifier = EntityIdentifierExtensions.FromString(candidate.Identifier);
        return targetIdentifier is
            EntityIdentifier.Villager or
            EntityIdentifier.VillagerV2 or
            EntityIdentifier.WanderingTrader or
            EntityIdentifier.IronGolem or
            EntityIdentifier.SnowGolem or
            EntityIdentifier.Turtle;
    }

    private static bool IsCreativeOrSpectator(Entity candidate) {
        return candidate is Player player &&
            (player.Gamemode == GameType.Creative || player.Gamemode == GameType.Spectator);
    }

    private static bool MatchesEntry(
        Dimension dimension,
        Entity candidate,
        Vec3 sourcePosition,
        IReadOnlyList<NearestAttackableTargetEntry> entries,
        bool behaviorMustSee) {
        for (int i = 0; i < entries.Count; i++) {
            NearestAttackableTargetEntry entry = entries[i];
            if (entry.MaxDistance is int maxDistance &&
                DistanceSquared(sourcePosition, candidate.Position) > maxDistance * maxDistance) {
                continue;
            }

            bool mustSee = entry.MustSee ?? behaviorMustSee;
            if ((!mustSee || HasLineOfSight(dimension, sourcePosition, candidate)) &&
                MatchesFilters(candidate, entry.Filters)) {
                return true;
            }
        }

        return false;
    }

    internal static bool MatchesFilters(Entity candidate, IReadOnlyList<EntityTargetFilter> filters) {
        for (int i = 0; i < filters.Count; i++) {
            if (!MatchesFilter(candidate, filters[i])) {
                return false;
            }
        }

        return true;
    }

    private static bool MatchesFilter(Entity candidate, EntityTargetFilter filter) {
        if (filter.All.Count > 0 && !MatchesFilters(candidate, filter.All)) {
            return false;
        }

        if (filter.Any.Count > 0 && !filter.Any.Any(value => MatchesFilter(candidate, value))) {
            return false;
        }

        bool result = filter.Test is null || filter.Test switch {
            "is_family" => IsFamily(candidate, filter.Value),
            "has_component" => HasComponent(candidate, filter.Value),
            "in_water" => candidate.IsInWater == ReadBool(filter.Value),
            _ => false
        };

        return filter.Operator == 1 ? !result : result;
    }

    private static bool IsFamily(Entity candidate, JsonElement value) {
        if (value.ValueKind != JsonValueKind.String) {
            return false;
        }

        string family = value.GetString() ?? string.Empty;
        EntityIdentifier entityIdentifier = EntityIdentifierExtensions.FromString(candidate.Identifier);
        return family switch {
            "player" => candidate.IsPlayer(),
            "irongolem" => entityIdentifier == EntityIdentifier.IronGolem,
            "snowgolem" => entityIdentifier == EntityIdentifier.SnowGolem,
            "wolf" => entityIdentifier == EntityIdentifier.Wolf,
            "villager" => entityIdentifier is EntityIdentifier.Villager or EntityIdentifier.VillagerV2,
            "wandering_trader" => entityIdentifier == EntityIdentifier.WanderingTrader,
            "baby_turtle" => entityIdentifier == EntityIdentifier.Turtle,
            _ => candidate.Identifier.EndsWith($":{family}", StringComparison.Ordinal)
        };
    }

    private static bool HasComponent(Entity candidate, JsonElement value) {
        return value.ValueKind == JsonValueKind.String &&
            candidate.Type.Components.Contains(value.GetString() ?? string.Empty);
    }

    private static bool ReadBool(JsonElement value) {
        return value.ValueKind == JsonValueKind.True;
    }

    private void RequestPath(Dimension dimension, ulong currentTick) {
        if (_target is null || _pathPending) {
            return;
        }

        PathNode start = new(
            (int)MathF.Floor(Entity.Position.X),
            (int)MathF.Floor(Entity.Position.Y),
            (int)MathF.Floor(Entity.Position.Z));
        Vec3 targetPosition = GetFeetPosition(_target);
        _pathTargetX = targetPosition.X;
        _pathTargetY = targetPosition.Y;
        _pathTargetZ = targetPosition.Z;
        PathNode target = new(
            (int)MathF.Floor(targetPosition.X),
            (int)MathF.Floor(targetPosition.Y),
            (int)MathF.Floor(targetPosition.Z));
        int requestId = ++_pathRequestId;
        _nextPathRequest = currentTick + 20;
        _pathPending = true;
        _pathStale = false;
        dimension.RequestPath(start, target, path => {
            _pathPending = false;
            if (!Entity.IsAlive || Entity.Dimension != dimension || _target is null ||
                _target.Dimension != dimension || requestId != _pathRequestId) {
                return;
            }

            _path = path;
            _pathIndex = path is null ? 0 : Math.Min(1, path.Nodes.Count);
        }, radius: 16, verticalRange: 2, maxVisitedNodes: 2048, maxDistance: 16f);
    }

    private void FollowPath() {
        if (_path is null || _pathIndex >= _path.Nodes.Count) {
            return;
        }

        PathNode node = _path.Nodes[_pathIndex];
        float deltaX = node.X + 0.5f - Entity.Position.X;
        float deltaZ = node.Z + 0.5f - Entity.Position.Z;
        float distance = MathF.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        if (distance < 0.45f) {
            _pathIndex++;
            if (_pathIndex >= _path.Nodes.Count) {
                Stop();
                return;
            }

            node = _path.Nodes[_pathIndex];
            deltaX = node.X + 0.5f - Entity.Position.X;
            deltaZ = node.Z + 0.5f - Entity.Position.Z;
            distance = MathF.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        }

        if (distance <= 0.01f) {
            return;
        }

        float directionX = deltaX / distance;
        float directionZ = deltaZ / distance;
        if (distance < 1.25f && _pathIndex + 1 < _path.Nodes.Count) {
            PathNode nextNode = _path.Nodes[_pathIndex + 1];
            float nextX = nextNode.X + 0.5f - Entity.Position.X;
            float nextZ = nextNode.Z + 0.5f - Entity.Position.Z;
            float nextDistance = MathF.Sqrt(nextX * nextX + nextZ * nextZ);
            if (nextDistance > 0.01f) {
                float nextWeight = 1f - distance / 1.25f;
                directionX = directionX * (1f - nextWeight) + nextX / nextDistance * nextWeight;
                directionZ = directionZ * (1f - nextWeight) + nextZ / nextDistance * nextWeight;
                float directionLength = MathF.Sqrt(directionX * directionX + directionZ * directionZ);
                directionX /= directionLength;
                directionZ /= directionLength;
            }
        }

        float desiredX = directionX * _movementSpeed;
        float desiredZ = directionZ * _movementSpeed;
        Entity.Velocity = new Vec3 {
            X = Entity.Velocity.X + (desiredX - Entity.Velocity.X) * 0.35f,
            Y = Entity.Velocity.Y,
            Z = Entity.Velocity.Z + (desiredZ - Entity.Velocity.Z) * 0.35f
        };
        float yaw = MathF.Atan2(-deltaX, deltaZ) * (180f / MathF.PI);
        Entity.Rotation = new Vec3 {
            X = Entity.Rotation.X,
            Y = RotateTowards(Entity.Rotation.Y, yaw, 18f),
            Z = Entity.Rotation.Z
        };
    }

    private static void GatherEntities(Dimension dimension, Vec3 center, float radius, List<Entity> candidates) {
        int minChunkX = (int)MathF.Floor((center.X - radius) / 16f);
        int maxChunkX = (int)MathF.Floor((center.X + radius) / 16f);
        int minChunkZ = (int)MathF.Floor((center.Z - radius) / 16f);
        int maxChunkZ = (int)MathF.Floor((center.Z + radius) / 16f);

        for (int chunkX = minChunkX; chunkX <= maxChunkX; chunkX++) {
            for (int chunkZ = minChunkZ; chunkZ <= maxChunkZ; chunkZ++) {
                candidates.AddRange(dimension.GetEntities(chunkX, chunkZ));
            }
        }
    }

    private static bool HasLineOfSight(Dimension dimension, Vec3 source, Entity target) {
        Vec3 start = new() { X = source.X, Y = source.Y + 1.2f, Z = source.Z };
        Vec3 targetPosition = GetFeetPosition(target);
        float targetHeight = target.GetTrait<EntityCollisionTrait>()?.Height ?? EntityCollisionTrait.DefaultHeight;
        Vec3 end = new() {
            X = targetPosition.X,
            Y = targetPosition.Y + targetHeight * 0.8f,
            Z = targetPosition.Z
        };
        float dx = end.X - start.X;
        float dy = end.Y - start.Y;
        float dz = end.Z - start.Z;
        int steps = Math.Max(1, (int)MathF.Ceiling(MathF.Sqrt(dx * dx + dy * dy + dz * dz) * 8f));

        for (int i = 1; i < steps; i++) {
            float progress = i / (float)steps;
            float x = start.X + dx * progress;
            float y = start.Y + dy * progress;
            float z = start.Z + dz * progress;
            int blockX = (int)MathF.Floor(x);
            int blockY = (int)MathF.Floor(y);
            int blockZ = (int)MathF.Floor(z);

            if (!dimension.TryGetLoadedPermutation(blockX, blockY, blockZ, out BlockPermutation? permutation) ||
                permutation is null) {
                continue;
            }

            foreach (CollisionBox box in BlockCollisionShape.GetBoxes(permutation)) {
                float minX = blockX + (box.OriginX + 8f) / 16f;
                float minY = blockY + box.OriginY / 16f;
                float minZ = blockZ + (box.OriginZ + 8f) / 16f;
                if (x >= minX && x <= minX + box.SizeX / 16f &&
                    y >= minY && y <= minY + box.SizeY / 16f &&
                    z >= minZ && z <= minZ + box.SizeZ / 16f) {
                    return false;
                }
            }
        }

        return true;
    }

    private static float RotateTowards(float current, float target, float maximum) {
        float difference = MathF.IEEERemainder(target - current, 360f);
        return current + Math.Clamp(difference, -maximum, maximum);
    }

    private void Stop() {
        Entity.Velocity = new Vec3 {
            X = 0f,
            Y = Entity.Velocity.Y,
            Z = 0f
        };
    }

    private void LookAtTarget(Entity target) {
        EntityCollisionTrait? sourceCollision = Entity.GetTrait<EntityCollisionTrait>();
        EntityCollisionTrait? targetCollision = target.GetTrait<EntityCollisionTrait>();
        float sourceHeight = sourceCollision?.Height ?? EntityCollisionTrait.DefaultHeight;
        float targetHeight = targetCollision?.Height ?? EntityCollisionTrait.DefaultHeight;
        Vec3 targetPosition = GetFeetPosition(target);
        float dx = targetPosition.X - Entity.Position.X;
        float dy = targetPosition.Y + targetHeight * 0.8f - (Entity.Position.Y + sourceHeight * 0.8f);
        float dz = targetPosition.Z - Entity.Position.Z;
        float horizontalDistance = MathF.Sqrt(dx * dx + dz * dz);
        float yaw = MathF.Atan2(-dx, dz) * (180f / MathF.PI);
        float pitch = -MathF.Atan2(dy, MathF.Max(horizontalDistance, 0.001f)) * (180f / MathF.PI);

        Entity.Rotation = new Vec3 {
            X = RotateTowards(Entity.Rotation.X, pitch, 28f),
            Y = Entity.Rotation.Y,
            Z = RotateTowards(Entity.Rotation.Z, yaw, 45f)
        };
    }

    private static Vec3 GetFeetPosition(Entity entity) {
        return entity is Player player ? player.GetPosition() : entity.Position;
    }

    private static float DistanceSquared(Vec3 first, Vec3 second) {
        float x = first.X - second.X;
        float y = first.Y - second.Y;
        float z = first.Z - second.Z;
        return x * x + y * y + z * z;
    }
}
