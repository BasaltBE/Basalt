namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Entities.Behaviors;
using Basalt.Core.Pathfinding;
using Basalt.Core.Traits;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Entities.Traits.Types;
using BedrockProtocol.Types;

public sealed class EntityAvoidMobTypeTrait : EntityTrait {
    public new static string Identifier => "avoid_mob_type";
    public new static readonly string[] Components = ["minecraft:behavior.avoid_mob_type"];

    private const ulong ReselectInterval = 10;
    private const ulong PathRequestCooldown = 20;
    private readonly Random _random = new();
    private Entity? _threat;
    private Basalt.Core.Pathfinding.Path? _path;
    private int _pathIndex;
    private int _pathRequestId;
    private ulong _nextReselect;
    private ulong _nextPathRequest;
    private float _speedMultiplier;
    private float _movementSpeed;

    public Entity? Threat => _threat;

    public EntityAvoidMobTypeTrait(Entity entity) : base(entity) {
    }

    public override void OnTick(TraitOnTickDetails details) {
        if (!Entity.IsAlive || Entity.Dimension is not Dimension dimension || Entity.IsPlayer() ||
            Entity.GetTrait<EntityMovementTrait>() is not { Grounded: true } movement || movement.InLava) {
            return;
        }

        _movementSpeed = movement.AiMovementSpeed;

        AvoidMobTypeBehavior? behavior = Entity.Type.AvoidMobType;
        if (behavior is null) {
            return;
        }

        if (details.CurrentTick >= _nextReselect || _threat is null || !_threat.IsAlive) {
            _nextReselect = details.CurrentTick + ReselectInterval;
            Entity? threat = FindThreat(dimension, behavior, out float speedMultiplier);
            if (!ReferenceEquals(threat, _threat)) {
                _threat = threat;
                _speedMultiplier = speedMultiplier;
                _path = null;
                _pathIndex = 0;
                _pathRequestId++;
            }
        }

        if (_threat is null || _threat.Dimension != dimension) {
            Stop();
            return;
        }

        if (_path is null || _pathIndex >= _path.Nodes.Count) {
            if (details.CurrentTick >= _nextPathRequest) {
                RequestPath(dimension, details.CurrentTick);
            }

            Stop();
            return;
        }

        FollowPath();
    }

    public override void OnDespawn(EntityDespawnOptions details) {
        _threat = null;
        _path = null;
        _pathRequestId++;
    }

    public override EntityTrait Clone(Entity entity) {
        return new EntityAvoidMobTypeTrait(entity);
    }

    private Entity? FindThreat(
        Dimension dimension,
        AvoidMobTypeBehavior behavior,
        out float speedMultiplier) {
        Entity? nearest = null;
        float nearestDistance = float.MaxValue;
        speedMultiplier = 1f;

        foreach (Entity candidate in dimension.Entities) {
            if (ReferenceEquals(candidate, Entity) || !candidate.IsAlive || candidate.PendingDespawn) {
                continue;
            }

            foreach (AvoidMobTypeEntry entry in behavior.EntityTypes) {
                if (entry.MaxDistance is int maxDistance &&
                    DistanceSquared(Entity.Position, candidate.Position) > maxDistance * maxDistance) {
                    continue;
                }

                if (!EntityTargetingTrait.MatchesFilters(candidate, entry.Filters)) {
                    continue;
                }

                float distance = DistanceSquared(Entity.Position, candidate.Position);
                if (distance >= nearestDistance) {
                    continue;
                }

                nearest = candidate;
                nearestDistance = distance;
                speedMultiplier = MathF.Max(entry.WalkSpeedMultiplier, entry.SprintSpeedMultiplier);
                break;
            }
        }

        return nearest;
    }

    private void RequestPath(Dimension dimension, ulong currentTick) {
        if (_threat is null) {
            return;
        }

        float deltaX = Entity.Position.X - _threat.Position.X;
        float deltaZ = Entity.Position.Z - _threat.Position.Z;
        float length = MathF.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        if (length < 0.01f) {
            deltaX = 1f;
            deltaZ = 0f;
            length = 1f;
        }

        int distance = _random.Next(8, 13);
        PathNode start = new(
            (int)MathF.Floor(Entity.Position.X),
            (int)MathF.Floor(Entity.Position.Y),
            (int)MathF.Floor(Entity.Position.Z));
        PathNode target = new(
            (int)MathF.Floor(Entity.Position.X + deltaX / length * distance),
            (int)MathF.Floor(Entity.Position.Y),
            (int)MathF.Floor(Entity.Position.Z + deltaZ / length * distance));
        int requestId = ++_pathRequestId;
        _nextPathRequest = currentTick + PathRequestCooldown;
        dimension.RequestPath(start, target, path => {
            if (!Entity.IsAlive || Entity.Dimension != dimension || _threat is null ||
                _threat.Dimension != dimension || requestId != _pathRequestId) {
                return;
            }

            _path = path;
            _pathIndex = path is null ? 0 : Math.Min(1, path.Nodes.Count);
        }, radius: 12, verticalRange: 4, maxVisitedNodes: 1024, maxDistance: 12f);
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

        float scale = _movementSpeed * _speedMultiplier / distance;
        float desiredX = deltaX * scale;
        float desiredZ = deltaZ * scale;
        Entity.Velocity = new Vec3 {
            X = Entity.Velocity.X + (desiredX - Entity.Velocity.X) * 0.35f,
            Y = Entity.Velocity.Y,
            Z = Entity.Velocity.Z + (desiredZ - Entity.Velocity.Z) * 0.35f
        };
        float yaw = MathF.Atan2(-deltaX, deltaZ) * (180f / MathF.PI);
        Entity.Rotation = new Vec3 {
            X = Entity.Rotation.X,
            Y = yaw,
            Z = yaw
        };
    }

    private void Stop() {
        Entity.Velocity = new Vec3 {
            X = 0f,
            Y = Entity.Velocity.Y,
            Z = 0f
        };
    }

    private static float DistanceSquared(Vec3 first, Vec3 second) {
        float x = first.X - second.X;
        float y = first.Y - second.Y;
        float z = first.Z - second.Z;
        return x * x + y * y + z * z;
    }
}
