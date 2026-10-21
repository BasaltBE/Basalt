using System.Numerics;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Entities.Types.Components;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Entities.Traits;

[EntityTraitAppliesTo(
    Identifiers = ["minecraft:player"],
    Components = ["minecraft:movement", "minecraft:movement.basic", "minecraft:movement.generic"]
)]
public sealed class EntityMovementTrait(Entity entity) : EntityTrait(entity) {
    private const float CollisionEpsilon = 0.001f;
    internal const float PositionBroadcastThreshold = 0.01f;
    internal const float RotationBroadcastThreshold = 1;
    internal Vector3 BroadcastPosition;
    internal Vector2 BroadcastRotation;
    internal float BroadcastHeadRotation;
    internal bool BroadcastOnGround;
    public readonly Vec3 Motion = new();
    public float Gravity = 0.08f;
    public float Drag = 0.02f;
    public bool VerticalDragBeforeGravity;
    public float TerminalVelocity = -3.92f;
    public float MovementSpeed = 0.1f;
    public const float KnockbackHorizontal = 0.3f;
    public const float KnockbackVertical = 0.435f;
    public const float KnockbackVerticalLimit = 0.435f;
    internal bool Grounded;
    public bool Jumping;
    public bool HorizontalCollision;
    public bool VerticalCollision;
    public ulong? InputTick;

    public override void OnSpawn() {
        if (Entity is not Player)
            Entity.SetFlag(ActorFlag.HasGravity, true);
        BroadcastPosition = new Vector3(Entity.Position.X, Entity.Position.Y, Entity.Position.Z);
        BroadcastRotation = new Vector2(Entity.Rotation.X, Entity.Rotation.Y);
        BroadcastHeadRotation = Entity.HeadRotation;
        BroadcastOnGround = VerticalCollision && Motion.Y <= 0;
    }

    public override void OnRespawn() {
        Motion.X = 0;
        Motion.Y = 0;
        Motion.Z = 0;
        Grounded = false;
        Jumping = false;
        HorizontalCollision = false;
        VerticalCollision = false;
        BroadcastPosition = new Vector3(Entity.Position.X, Entity.Position.Y, Entity.Position.Z);
        BroadcastRotation = new Vector2(Entity.Rotation.X, Entity.Rotation.Y);
        BroadcastHeadRotation = Entity.HeadRotation;
        BroadcastOnGround = false;
    }

    public override void OnAdd() {
        if (Entity.Type?.PropertiesPayload?.Components.Movement is { } movement) {
            EntityNumericValue? value = movement.Value;
            double? speed = value?.Value ?? value?.Range?.RangeMax;
            if (speed is double configuredSpeed && double.IsFinite(configuredSpeed) &&
                configuredSpeed >= 0 && configuredSpeed <= float.MaxValue)
                MovementSpeed = (float)configuredSpeed;
        }

        float maximum = Entity.Type?.PropertiesPayload?.Components.Movement?.Max is double max &&
            double.IsFinite(max) && max >= MovementSpeed && max <= float.MaxValue
                ? (float)max
                : Math.Max(MovementSpeed, 1f);
        Entity.Attributes.Set(
            "minecraft:movement",
            0,
            maximum,
            MovementSpeed,
            0,
            maximum,
            MovementSpeed
        );
    }

    public override void Tick() {
        if (Entity is Player || Entity.Dead || Entity.SpawnedDimension is not Dimension dimension)
            return;

        bool gravity = Entity.Flags.Get(ActorFlag.HasGravity);
        if (!gravity && Motion.X == 0 && Motion.Y == 0 && Motion.Z == 0)
            return;

        float width = 0.6f;
        float height = 1.8f;
        if (Entity is ItemEntity) {
            width = 0.25f;
            height = 0.25f;
        }
        else if (Entity.Type?.PropertiesPayload?.Components.CollisionBox is { } collisionBox) {
            if (collisionBox.Width is double configuredWidth && double.IsFinite(configuredWidth) &&
                configuredWidth > 0 && configuredWidth <= float.MaxValue)
                width = (float)configuredWidth;
            if (collisionBox.Height is double configuredHeight && double.IsFinite(configuredHeight) &&
                configuredHeight > 0 && configuredHeight <= float.MaxValue)
                height = (float)configuredHeight;
        }
        float drag = Math.Clamp(Drag, 0, 1);
        float verticalDrag = 1 - drag;
        float horizontalDrag = verticalDrag;
        if (Grounded) {
            int blockX = (int)MathF.Floor(Entity.Position.X);
            int blockY = (int)MathF.Floor(Entity.Position.Y - CollisionEpsilon);
            int blockZ = (int)MathF.Floor(Entity.Position.Z);
            BlockType? surface = BlockPalette.FindType(dimension.GetPermutation(blockX, blockY, blockZ).Identifier);
            float friction = surface is { Solid: true } && float.IsFinite(surface.Friction)
                ? Math.Clamp(surface.Friction, 0.6f, 0.99f)
                : 0.6f;
            horizontalDrag *= friction;
        }

        float velocityX = Motion.X * horizontalDrag;
        float velocityY = Motion.Y;
        float velocityZ = Motion.Z * horizontalDrag;
        if (gravity) {
            velocityY = VerticalDragBeforeGravity
                ? velocityY * verticalDrag - Math.Max(0, Gravity)
                : (velocityY - Math.Max(0, Gravity)) * verticalDrag;
            velocityY = Math.Max(velocityY, TerminalVelocity);
        }
        else if (VerticalDragBeforeGravity) {
            velocityY *= verticalDrag;
        }

        float previousX = Entity.Position.X;
        float previousY = Entity.Position.Y;
        float previousZ = Entity.Position.Z;
        float nextX = Entity.Position.X + velocityX;
        if (velocityX != 0 && Collides(dimension, nextX, Entity.Position.Y, Entity.Position.Z, width, height))
            velocityX = 0;
        else
            Entity.Position.X = nextX;

        float nextZ = Entity.Position.Z + velocityZ;
        if (velocityZ != 0 && Collides(dimension, Entity.Position.X, Entity.Position.Y, nextZ, width, height))
            velocityZ = 0;
        else
            Entity.Position.Z = nextZ;

        float nextY = Entity.Position.Y + velocityY;
        if (Collides(dimension, Entity.Position.X, nextY, Entity.Position.Z, width, height)) {
            if (velocityY < 0) {
                float landedY = float.NegativeInfinity;
                int minX = (int)MathF.Floor(Entity.Position.X - width * 0.5f + CollisionEpsilon);
                int maxX = (int)MathF.Floor(Entity.Position.X + width * 0.5f - CollisionEpsilon);
                int minZ = (int)MathF.Floor(Entity.Position.Z - width * 0.5f + CollisionEpsilon);
                int maxZ = (int)MathF.Floor(Entity.Position.Z + width * 0.5f - CollisionEpsilon);
                int minY = (int)MathF.Floor(nextY + CollisionEpsilon);
                int maxY = (int)MathF.Floor(Entity.Position.Y + CollisionEpsilon);
                for (int x = minX; x <= maxX; x++) {
                    for (int z = minZ; z <= maxZ; z++) {
                        for (int y = minY; y <= maxY; y++) {
                            if (BlockPalette.FindType(dimension.GetPermutation(x, y, z).Identifier)?.Solid == true)
                                landedY = Math.Max(landedY, y + 1);
                        }
                    }
                }

                if (float.IsFinite(landedY))
                    Entity.Position.Y = landedY;
                Grounded = true;
            }
            velocityY = 0;
        }
        else {
            Entity.Position.Y = nextY;
            Grounded = false;
        }

        Motion.X = MathF.Abs(velocityX) < 0.003f ? 0 : velocityX;
        Motion.Y = velocityY;
        Motion.Z = MathF.Abs(velocityZ) < 0.003f ? 0 : velocityZ;
        if (Entity.Position.X != previousX || Entity.Position.Y != previousY || Entity.Position.Z != previousZ)
            Entity.OnMove();
    }

    internal bool CalculateKnockback(Entity source, Vec3 velocity) {
        float resistance = Math.Clamp(Entity.Attributes.Get("minecraft:knockback_resistance")?.Current ?? 0, 0, 1);
        float strength = 1 - resistance;
        if (strength == 0)
            return false;

        float x = Entity.Position.X - source.Position.X;
        float z = Entity.Position.Z - source.Position.Z;
        float length = MathF.Sqrt(x * x + z * z);
        if (length <= 0.0001f) {
            float yaw = source.Rotation.Y * (MathF.PI / 180);
            x = -MathF.Sin(yaw);
            z = MathF.Cos(yaw);
            length = 1;
        }

        float inverseLength = 1 / length;
        velocity.X = Motion.X * 0.5f + x * inverseLength * KnockbackHorizontal * strength;
        velocity.Y = MathF.Min(Motion.Y * 0.5f + KnockbackVertical * strength, KnockbackVerticalLimit);
        velocity.Z = Motion.Z * 0.5f + z * inverseLength * KnockbackHorizontal * strength;
        return true;
    }

    private static bool Collides(Dimension dimension, float x, float y, float z, float width, float height) {
        int minX = (int)MathF.Floor(x - width * 0.5f + CollisionEpsilon);
        int maxX = (int)MathF.Floor(x + width * 0.5f - CollisionEpsilon);
        int minY = (int)MathF.Floor(y + CollisionEpsilon);
        int maxY = (int)MathF.Floor(y + height - CollisionEpsilon);
        int minZ = (int)MathF.Floor(z - width * 0.5f + CollisionEpsilon);
        int maxZ = (int)MathF.Floor(z + width * 0.5f - CollisionEpsilon);
        for (int blockX = minX; blockX <= maxX; blockX++) {
            for (int blockY = minY; blockY <= maxY; blockY++) {
                for (int blockZ = minZ; blockZ <= maxZ; blockZ++) {
                    if (dimension.GetChunk(blockX >> 4, blockZ >> 4) is null ||
                        BlockPalette.FindType(dimension.GetPermutation(blockX, blockY, blockZ).Identifier)?.Solid == true)
                        return true;
                }
            }
        }
        return false;
    }
}
