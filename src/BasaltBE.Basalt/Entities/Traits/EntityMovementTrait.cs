using System.Numerics;
using BasaltBE.Nbt.Types;
using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Basalt.Entities.Enums;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Entities.Types.Components;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Types;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Entities.Traits;

[EntityTraitAppliesTo(
    Identifiers = ["minecraft:player", "minecraft:xp_orb", "minecraft:tnt"],
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
    bool smallEntity;
    public bool Jumping;
    public bool HorizontalCollision;
    public bool VerticalCollision;
    public ulong? InputTick;

    public override void Read(CompoundTag tag) {
        if (Entity is Player)
            return;
        Grounded = tag.Get<ByteTag>("OnGround") is { Value: not 0 };
        VerticalCollision = Grounded;
    }

    public override void Write(CompoundTag tag) {
        if (Entity is not Player)
            tag.Add("OnGround", new ByteTag { Value = Grounded ? (byte)1 : (byte)0 });
    }

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
        smallEntity = Entity is ItemEntity || Entity.GetTrait<ExperienceOrbTrait>() is not null;
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

        FluidContact fluid = Entity.GetFluidContact();
        bool water = fluid.WaterDepth > 0;
        bool lava = !water && fluid.LavaDepth > 0;
        bool gravity = Entity.Flags.Get(ActorFlag.HasGravity);
        if (!gravity && Motion.X == 0 && Motion.Y == 0 && Motion.Z == 0 && !water && !lava)
            return;

        float width = Entity.CollisionWidth;
        float height = Entity.GetCollisionHeight();
        bool living = !smallEntity && (Entity.Type?.PropertiesPayload?.Components.Health is not null ||
            Entity.GetTrait<EntityHealthTrait>() is not null);
        float drag = Math.Clamp(Drag, 0, 1);
        float verticalDrag = 1 - drag;
        float horizontalDrag = verticalDrag;
        if (Grounded && !water && !lava) {
            int blockX = (int)MathF.Floor(Entity.Position.X);
            int blockY = (int)MathF.Floor(Entity.Position.Y - CollisionEpsilon);
            int blockZ = (int)MathF.Floor(Entity.Position.Z);
            BlockType? surface = BlockPalette.FindType(dimension.GetPermutation(blockX, blockY, blockZ).Identifier);
            horizontalDrag *= surface is { Solid: true } && float.IsFinite(surface.Friction)
                ? Math.Clamp(surface.Friction, 0.6f, 0.99f)
                : 0.6f;
        }

        Vector3 velocity = new(Motion.X, Motion.Y, Motion.Z);
        if (water || lava) {
            Vector3 current = water ? fluid.WaterFlow : fluid.LavaFlow;
            if (!smallEntity && current.LengthSquared() > 0)
                current = Vector3.Normalize(current);
            float acceleration = water ? 0.014f : dimension.Type == DimensionType.Nether ? 0.007f : 0.0023333334f;
            float depth = water ? fluid.WaterDepth : fluid.LavaDepth;
            velocity += current * (acceleration * Math.Min(1, depth / 0.4f));
        }
        if (smallEntity && (water || lava)) {
            float fluidDrag = (water ? 0.99f : 0.95f) * verticalDrag;
            velocity.X *= fluidDrag;
            velocity.Z *= fluidDrag;
            if (gravity) {
                if ((water ? fluid.WaterDepth : fluid.LavaDepth) <= 0.1f)
                    velocity.Y = -0.02f;
                else if (velocity.Y < 0.06f)
                    velocity.Y += 0.0005f;
            }
            velocity.Y *= verticalDrag;
        }
        else if ((water || lava) && living) {
            float slowdown = water ? 0.8f : 0.5f;
            velocity *= slowdown;
            if (gravity)
                velocity.Y -= 0.02f;
            if (Jumping)
                velocity.Y += 0.04f;
        }
        else {
            velocity.X *= horizontalDrag;
            velocity.Z *= horizontalDrag;
            if (gravity) {
                velocity.Y = VerticalDragBeforeGravity
                    ? velocity.Y * verticalDrag - Math.Max(0, Gravity)
                    : (velocity.Y - Math.Max(0, Gravity)) * verticalDrag;
                velocity.Y = Math.Max(velocity.Y, TerminalVelocity);
            }
            else if (VerticalDragBeforeGravity) {
                velocity.Y *= verticalDrag;
            }
        }

        Vector3 position = new(Entity.Position.X, Entity.Position.Y, Entity.Position.Z);
        Vector3 moved = ClipMotion(dimension, position, velocity, width, height);
        if (living && (Grounded || velocity.Y < 0 && moved.Y != velocity.Y) &&
            (moved.X != velocity.X || moved.Z != velocity.Z)) {
            Vector3 up = ClipMotion(dimension, position, new Vector3(0, 0.6f, 0), width, height);
            Vector3 across = ClipMotion(dimension, position + up, new Vector3(velocity.X, 0, velocity.Z), width, height);
            Vector3 down = ClipMotion(dimension, position + up + across, new Vector3(0, velocity.Y - up.Y, 0), width, height);
            if (across.X * across.X + across.Z * across.Z > moved.X * moved.X + moved.Z * moved.Z)
                moved = up + across + down;
        }
        HorizontalCollision = moved.X != velocity.X || moved.Z != velocity.Z;
        VerticalCollision = moved.Y != velocity.Y;
        Grounded = VerticalCollision && velocity.Y < 0;
        Motion.X = moved.X != velocity.X || MathF.Abs(velocity.X) < 0.003f && !water && !lava ? 0 : velocity.X;
        Motion.Y = VerticalCollision ? 0 : velocity.Y;
        Motion.Z = moved.Z != velocity.Z || MathF.Abs(velocity.Z) < 0.003f && !water && !lava ? 0 : velocity.Z;
        if (moved != Vector3.Zero) {
            Entity.Position.X += moved.X;
            Entity.Position.Y += moved.Y;
            Entity.Position.Z += moved.Z;
            Entity.OnMove();
        }
    }

    static Vector3 ClipMotion(Dimension dimension, Vector3 position, Vector3 motion, float width, float height) {
        Vector3 minimum = new(position.X - width * 0.5f, position.Y, position.Z - width * 0.5f);
        Vector3 maximum = new(position.X + width * 0.5f, position.Y + height, position.Z + width * 0.5f);
        Span<BlockCollisionBox> boxes = stackalloc BlockCollisionBox[5];
        for (int pass = 0; pass < 3; pass++) {
            int axis = pass == 0 ? 1 : pass == 1 ? 0 : 2;
            float delta = motion[axis];
            if (delta == 0)
                continue;
            Vector3 sweptMinimum = minimum;
            Vector3 sweptMaximum = maximum;
            sweptMinimum[axis] += Math.Min(0, delta);
            sweptMaximum[axis] += Math.Max(0, delta);
            int minX = (int)MathF.Floor(sweptMinimum.X + CollisionEpsilon);
            int maxX = (int)MathF.Floor(sweptMaximum.X - CollisionEpsilon);
            int minY = Math.Max(dimension.MinimumY, (int)MathF.Floor(sweptMinimum.Y - 0.5f + CollisionEpsilon));
            int maxY = Math.Min(dimension.MaximumY, (int)MathF.Floor(sweptMaximum.Y - CollisionEpsilon));
            int minZ = (int)MathF.Floor(sweptMinimum.Z + CollisionEpsilon);
            int maxZ = (int)MathF.Floor(sweptMaximum.Z - CollisionEpsilon);
            for (int x = minX; x <= maxX && delta != 0; x++) {
                for (int z = minZ; z <= maxZ && delta != 0; z++) {
                    bool loaded = dimension.GetChunk(x >> 4, z >> 4) is not null;
                    for (int y = minY; y <= maxY && delta != 0; y++) {
                        int count;
                        if (loaded) {
                            count = dimension.GetPermutation(x, y, z).GetCollisionBoxes(boxes);
                        }
                        else {
                            boxes[0] = new(0, 0, 0, 1, 1, 1);
                            count = 1;
                        }
                        for (int boxIndex = 0; boxIndex < count; boxIndex++) {
                            BlockCollisionBox box = boxes[boxIndex];
                            Vector3 blockMin = new(x + box.MinX, y + box.MinY, z + box.MinZ);
                            Vector3 blockMax = new(x + box.MaxX, y + box.MaxY, z + box.MaxZ);
                            int first = (axis + 1) % 3;
                            int second = (axis + 2) % 3;
                            if (maximum[first] <= blockMin[first] + CollisionEpsilon || minimum[first] >= blockMax[first] - CollisionEpsilon ||
                                maximum[second] <= blockMin[second] + CollisionEpsilon || minimum[second] >= blockMax[second] - CollisionEpsilon)
                                continue;
                            if (delta > 0 && maximum[axis] <= blockMin[axis] + CollisionEpsilon)
                                delta = Math.Min(delta, Math.Max(0, blockMin[axis] - maximum[axis]));
                            else if (delta < 0 && minimum[axis] >= blockMax[axis] - CollisionEpsilon)
                                delta = Math.Max(delta, Math.Min(0, blockMax[axis] - minimum[axis]));
                        }
                    }
                }
            }
            minimum[axis] += delta;
            maximum[axis] += delta;
            motion[axis] = delta;
        }
        return motion;
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

}
