namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Pathfinding;
using Basalt.Core.Traits;
using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Entities.Traits.Attribute;
using Basalt.BedrockProtocol.Types;
using Basalt.BedrockProtocol.Enums;
using System.Text.Json;

public sealed class EntityWanderTrait : EntityTrait {
    public new static string Identifier => "wander";
    public new static readonly string[] Components = [
        "minecraft:behavior.random_stroll",
        "minecraft:behavior.panic"
    ];

    private const ulong PathRequestCooldown = 20;
    private const ulong MinimumIdleTicks = 20;
    private const ulong MaximumIdleTicks = 80;
    private const float JumpVelocity = 0.42f;
    private const float ExternalImpulseSpeed = 0.18f;
    private const ulong KnockbackPauseTicks = 8;
    private const ulong PanicTicks = 100;
    private readonly Random _random = new();
    private Basalt.Core.Pathfinding.Path? _path;
    private int _pathIndex;
    private bool _pathPending;
    private ulong _nextPathRequest;
    private ulong _idleUntil;
    private ulong _lastTick;
    private ulong _panicUntil;
    private ulong _knockbackUntil;
    private int _pathRequestId;
    private float? _lastHealth;
    private bool _randomStroll;
    private bool _panic;
    private bool _groundMovement;
    private float _randomStrollSpeed = 1f;
    private float _panicSpeed = 1.25f;
    private bool _panicAllDamage;
    private readonly HashSet<ActorDamageCause> _panicDamageSources = [];

    public EntityWanderTrait(Entity entity) : base(entity) {
    }

    public override void OnAdd() {
        _groundMovement = Entity.Type.Components.Contains("minecraft:movement.basic");
        _randomStroll = ReadBehavior("minecraft:behavior.random_stroll", out JsonElement randomStroll);
        _panic = ReadBehavior("minecraft:behavior.panic", out JsonElement panic);
        _randomStrollSpeed = ReadMultiplier(randomStroll, 1f);
        _panicSpeed = ReadMultiplier(panic, 1.25f);

        if (!_panic || !panic.TryGetProperty("damage_sources", out JsonElement sources) ||
            sources.ValueKind != JsonValueKind.Array) {
            return;
        }

        foreach (JsonElement source in sources.EnumerateArray()) {
            if (source.ValueKind != JsonValueKind.String || source.GetString() is not string value) {
                continue;
            }

            if (string.Equals(value, "all", StringComparison.Ordinal)) {
                _panicAllDamage = true;
                continue;
            }

            ActorDamageCause? cause = value switch {
                "fire" => ActorDamageCause.Fire,
                "fire_tick" => ActorDamageCause.FireTick,
                "magma" => ActorDamageCause.Lava,
                _ => null
            };
            if (cause is ActorDamageCause damageCause) {
                _panicDamageSources.Add(damageCause);
            }
        }
    }

    public override void OnTick(TraitOnTickDetails details) {
        if (!Entity.IsAlive || Entity.Dimension is null || Entity.IsPlayer() ||
            Entity.Identifier == EntityIdentifier.Item.ToIdentifierString()) {
            return;
        }

        EntityMovementTrait? movement = Entity.GetTrait<EntityMovementTrait>();
        if (movement is null || movement.InLava || Entity.IsInWater || !_groundMovement) {
            return;
        }

        if (Entity.GetTrait<EntityTargetingTrait>()?.Target is not null) {
            return;
        }

        if (Entity.GetTrait<EntityAvoidMobTypeTrait>()?.Threat is not null) {
            return;
        }

        _lastTick = details.CurrentTick;
        EntityHealthTrait? health = Entity.GetTrait<EntityHealthTrait>();
        if (health is not null) {
            if (_lastHealth is float lastHealth && health.CurrentValue < lastHealth &&
                ShouldPanic(health.LastDamageCause)) {
                _panicUntil = details.CurrentTick + PanicTicks;
                _path = null;
                _pathIndex = 0;
                _idleUntil = details.CurrentTick;
                _nextPathRequest = details.CurrentTick;
                _pathRequestId++;
            }

            _lastHealth = health.CurrentValue;
        }

        float horizontalSpeed = MathF.Sqrt(
            Entity.Velocity.X * Entity.Velocity.X +
            Entity.Velocity.Z * Entity.Velocity.Z);
        if (horizontalSpeed > ExternalImpulseSpeed) {
            _knockbackUntil = details.CurrentTick + KnockbackPauseTicks;
            return;
        }

        if (details.CurrentTick < _knockbackUntil) {
            return;
        }

        if (!movement.Grounded) {
            return;
        }

        if (!_randomStroll && details.CurrentTick >= _panicUntil) {
            return;
        }

        if (_path is null || _pathIndex >= _path.Nodes.Count) {
            Entity.Velocity = new Vec3 {
                X = 0f,
                Y = Entity.Velocity.Y,
                Z = 0f
            };

            if (!_pathPending && details.CurrentTick >= _nextPathRequest &&
                details.CurrentTick >= _idleUntil) {
                RequestWanderPath(details.CurrentTick);
            }

            return;
        }

        PathNode node = _path.Nodes[_pathIndex];
        float targetX = node.X + 0.5f;
        float targetZ = node.Z + 0.5f;
        float deltaX = targetX - Entity.Position.X;
        float deltaZ = targetZ - Entity.Position.Z;
        float distance = MathF.Sqrt(deltaX * deltaX + deltaZ * deltaZ);

        if (distance < 0.45f && MathF.Abs(node.Y - Entity.Position.Y) < 1.25f) {
            _pathIndex++;
            if (_pathIndex >= _path.Nodes.Count) {
                FinishPath(details.CurrentTick);
                return;
            }

            node = _path.Nodes[_pathIndex];
            targetX = node.X + 0.5f;
            targetZ = node.Z + 0.5f;
            deltaX = targetX - Entity.Position.X;
            deltaZ = targetZ - Entity.Position.Z;
            distance = MathF.Sqrt(deltaX * deltaX + deltaZ * deltaZ);
        }

        if (distance <= 0.01f) {
            return;
        }

        float speedMultiplier = details.CurrentTick < _panicUntil ? _panicSpeed : _randomStrollSpeed;
        float speed = movement.AiMovementSpeed * speedMultiplier;
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

        float velocityY = Entity.Velocity.Y;
        if (node.Y > MathF.Floor(Entity.Position.Y) && movement is { Grounded: true } && velocityY <= 0f) {
            velocityY = JumpVelocity;
        }

        float desiredX = directionX * speed;
        float desiredZ = directionZ * speed;
        Entity.Velocity = new Vec3 {
            X = Entity.Velocity.X + (desiredX - Entity.Velocity.X) * 0.35f,
            Y = velocityY,
            Z = Entity.Velocity.Z + (desiredZ - Entity.Velocity.Z) * 0.35f
        };

        float yaw = MathF.Atan2(-deltaX, deltaZ) * (180f / MathF.PI);
        Entity.Rotation = new Vec3 {
            X = Entity.Rotation.X,
            Y = RotateTowards(Entity.Rotation.Y, yaw, 18f),
            Z = RotateTowards(Entity.Rotation.Z, yaw, 30f)
        };
    }

    public override void OnHurt(EntityHurtDetails details) {
        if (!ShouldPanic(details.Cause) || Entity.Dimension?.World is not Tickable tickable) {
            return;
        }

        BeginPanic(tickable.TickValue);
    }

    public override void OnDespawn(EntityDespawnOptions details) {
        _path = null;
        _pathIndex = 0;
        _pathPending = false;
        _idleUntil = 0;
        _pathRequestId++;
        _lastHealth = null;
    }

    public override EntityTrait Clone(Entity entity) {
        return new EntityWanderTrait(entity);
    }

    private void RequestWanderPath(ulong currentTick) {
        Dimension dimension = Entity.Dimension!;
        int startX = (int)MathF.Floor(Entity.Position.X);
        int startY = (int)MathF.Floor(Entity.Position.Y);
        int startZ = (int)MathF.Floor(Entity.Position.Z);
        int distance = _random.Next(4, 11);
        if (currentTick < _panicUntil) {
            distance = _random.Next(8, 15);
        }

        int targetX = startX + _random.Next(-distance, distance + 1);
        int targetZ = startZ + _random.Next(-distance, distance + 1);
        PathNode start = new(startX, startY, startZ);
        PathNode target = new(targetX, startY, targetZ);

        _nextPathRequest = currentTick + PathRequestCooldown;
        _pathPending = true;
        int pathRequestId = ++_pathRequestId;
        dimension.RequestPath(start, target, path => {
            _pathPending = false;
            if (!Entity.IsAlive || Entity.Dimension != dimension || _lastTick < currentTick ||
                pathRequestId != _pathRequestId) {
                return;
            }

            _path = path;
            if (path is null || path.Nodes.Count <= 1) {
                _path = null;
                _pathIndex = 0;
                _idleUntil = currentTick < _panicUntil ? currentTick : currentTick + NextIdleTicks();
                return;
            }

            _pathIndex = 1;
        }, radius: 12, verticalRange: 6, maxVisitedNodes: 1024, maxDistance: 12f);
    }

    private void FinishPath(ulong currentTick) {
        _path = null;
        _pathIndex = 0;
        _idleUntil = currentTick < _panicUntil ? currentTick : currentTick + NextIdleTicks();
        _nextPathRequest = _idleUntil + PathRequestCooldown;
    }

    private void BeginPanic(ulong currentTick) {
        _panicUntil = currentTick + PanicTicks;
        _path = null;
        _pathIndex = 0;
        _idleUntil = currentTick;
        _nextPathRequest = currentTick;
        _pathRequestId++;
    }

    private ulong NextIdleTicks() {
        return (ulong)_random.Next((int)MinimumIdleTicks, (int)MaximumIdleTicks + 1);
    }

    private bool ShouldPanic(ActorDamageCause? cause) {
        return _panic && (_panicAllDamage || cause is ActorDamageCause damage && _panicDamageSources.Contains(damage));
    }

    private bool ReadBehavior(string identifier, out JsonElement properties) {
        if (!Entity.Type.Components.Contains(identifier)) {
            properties = default;
            return false;
        }

        return Entity.Type.TryGetComponentProperties(identifier, out properties);
    }

    private static float ReadMultiplier(JsonElement properties, float fallback) {
        if (properties.ValueKind == JsonValueKind.Object &&
            properties.TryGetProperty("speed_multiplier", out JsonElement multiplier) &&
            multiplier.ValueKind == JsonValueKind.Number &&
            multiplier.TryGetSingle(out float value) && value > 0f) {
            return value;
        }

        return fallback;
    }

    private static float RotateTowards(float current, float target, float maximum) {
        float difference = MathF.IEEERemainder(target - current, 360f);
        return current + Math.Clamp(difference, -maximum, maximum);
    }
}
