namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Blocks.Traits;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Profiling;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.Core.Traits;

public sealed class EntityMovementTrait : EntityTrait {
    public new static string Identifier => "movement";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player, EntityIdentifier.Item];
    public new static readonly string[] Components = ["minecraft:movement", "minecraft:movement.basic", "minecraft:movement.jump"];

    public static float BaseMovementSpeed => 0.1f;
    public static float BaseUnderwaterMovementSpeed => 0.02f;
    public static float BaseLavaMovementSpeed => 0.02f;


    public float Speed { get; private set; } = 1f;
    private float _fallDistance;
    public float GravityPerTick { get; set; } = 0.08f;
    public float Drag { get; set; } = 0.98f;
    public float TerminalVelocity { get; set; } = -3.92f;
    public float GroundFriction { get; set; } = 0.6f;
    public float MinHorizontalVelocity { get; set; } = 0.01f;
    public float WaterCurrentForce { get; set; } = 0.04f;
    private const float CollisionEpsilon = 0.001f;


    public EntityMovementTrait(Entity entity) : base(entity) { }



    // public override void OnTick(TraitOnTickDetails details) {}

    public void SetSpeed(float speed = 1f) {
        Speed = speed;

        float movement = BaseMovementSpeed * Speed;
        float underwater = BaseUnderwaterMovementSpeed * Speed;
        float lava = BaseLavaMovementSpeed * Speed;

        SetAttribute(AttributeName.Movement, movement, BaseMovementSpeed);
        SetAttribute(AttributeName.UnderwaterMovement, underwater, BaseUnderwaterMovementSpeed);
        SetAttribute(AttributeName.LavaMovement, lava, BaseLavaMovementSpeed);
    }

    public override void OnAdd() {
        SetSpeed(Speed);
    }

    public override void OnSpawn(EntitySpawnOptions details) {
        if (!Entity.Flags.GetActorFlag(ActorFlag.HasGravity)) {
            Entity.Flags.SetActorFlag(ActorFlag.HasGravity, true);
        }

        _fallDistance = 0f;
    }

    public override void OnTick(TraitOnTickDetails details) {
        if (!Entity.IsAlive || Entity.Dimension is null || Entity.IsPlayer()) {
            return;
        }

        using var __zone = Profiler.Enabled ? Profiler.BeginZone("EntityMovement.OnTick") : default;
        Vec3f previousPosition = Entity.Position;
        EntityCollisionTrait? collision = Entity.GetTrait<EntityCollisionTrait>();
        if (collision is not null) {
            collision.XAxisCollision = 0;
            collision.YAxisCollision = 0;
            collision.ZAxisCollision = 0;
        }

        for (uint i = 0; i < details.DeltaTick; i++) {
            float flowX = 0f;
            float flowY = 0f;
            float flowZ = 0f;
            float halfWidth = CollisionWidth() * 0.5f;
            int minWaterX = (int)MathF.Floor(Entity.Position.X - halfWidth + CollisionEpsilon);
            int maxWaterX = (int)MathF.Floor(Entity.Position.X + halfWidth - CollisionEpsilon);
            int minWaterY = (int)MathF.Floor(Entity.Position.Y + CollisionEpsilon);
            int maxWaterY = (int)MathF.Floor(Entity.Position.Y + CollisionHeight() - CollisionEpsilon);
            int minWaterZ = (int)MathF.Floor(Entity.Position.Z - halfWidth + CollisionEpsilon);
            int maxWaterZ = (int)MathF.Floor(Entity.Position.Z + halfWidth - CollisionEpsilon);

            for (int waterX = minWaterX; waterX <= maxWaterX; waterX++) {
                for (int waterY = minWaterY; waterY <= maxWaterY; waterY++) {
                    for (int waterZ = minWaterZ; waterZ <= maxWaterZ; waterZ++) {
                        Vec3f flow = FluidTrait.GetWaterFlow(
                            Entity.Dimension,
                            new BlockPos { X = waterX, Y = waterY, Z = waterZ },
                            out float waterHeight);

                        if (waterHeight == 0f || Entity.Position.Y >= waterY + waterHeight) {
                            continue;
                        }

                        flowX += flow.X;
                        flowY += flow.Y;
                        flowZ += flow.Z;
                    }
                }
            }

            float flowLength = MathF.Sqrt((flowX * flowX) + (flowY * flowY) + (flowZ * flowZ));
            if (flowLength > 0f) {
                Entity.Velocity = new Vec3f {
                    X = Entity.Velocity.X + (flowX / flowLength * WaterCurrentForce),
                    Y = Entity.Velocity.Y + (flowY / flowLength * WaterCurrentForce),
                    Z = Entity.Velocity.Z + (flowZ / flowLength * WaterCurrentForce)
                };
            }

            bool applyGravity = Entity.Flags.GetActorFlag(ActorFlag.HasGravity) && !Entity.IsSwimming;
            if (applyGravity) {
                Entity.Velocity = new Vec3f {
                    X = Entity.Velocity.X,
                    Y = Entity.Velocity.Y - GravityPerTick,
                    Z = Entity.Velocity.Z
                };
                Entity.Velocity = new Vec3f {
                    X = Entity.Velocity.X,
                    Y = Entity.Velocity.Y * Drag,
                    Z = Entity.Velocity.Z
                };
                Entity.Velocity = new Vec3f {
                    X = Entity.Velocity.X * Drag,
                    Y = Entity.Velocity.Y,
                    Z = Entity.Velocity.Z * Drag
                };
                if (Entity.Velocity.Y < TerminalVelocity) {
                    Entity.Velocity = new Vec3f {
                        X = Entity.Velocity.X,
                        Y = TerminalVelocity,
                        Z = Entity.Velocity.Z
                    };
                }
            }

            float velocityX = Entity.Velocity.X;
            float velocityZ = Entity.Velocity.Z;
            float nextX = Entity.Position.X + velocityX;
            float nextY = Entity.Position.Y + Entity.Velocity.Y;
            float nextZ = Entity.Position.Z + velocityZ;
            if (velocityX != 0f && CollidesWithSolidBlocks(nextX, Entity.Position.Y, Entity.Position.Z)) {
                if (collision is not null) {
                    collision.XAxisCollision = velocityX > 0f ? 1 : -1;
                }

                nextX = Entity.Position.X;
                velocityX = 0f;
            }

            if (velocityZ != 0f && CollidesWithSolidBlocks(nextX, Entity.Position.Y, nextZ)) {
                if (collision is not null) {
                    collision.ZAxisCollision = velocityZ > 0f ? 1 : -1;
                }

                nextZ = Entity.Position.Z;
                velocityZ = 0f;
            }

            if (applyGravity && Entity.Velocity.Y <= 0f && IsGrounded(nextX, nextY, nextZ)) {
                if (collision is not null) {
                    collision.YAxisCollision = -1;
                }

                float landingY = FindGroundSurface(nextX, Entity.Position.Y, nextY, nextZ);

                float groundedVelocityX = velocityX * GroundFriction;
                float groundedVelocityZ = velocityZ * GroundFriction;
                if (MathF.Abs(groundedVelocityX) < MinHorizontalVelocity) {
                    groundedVelocityX = 0f;
                }
                if (MathF.Abs(groundedVelocityZ) < MinHorizontalVelocity) {
                    groundedVelocityZ = 0f;
                }

                Entity.Position = new Vec3f {
                    X = nextX,
                    Y = landingY,
                    Z = nextZ
                };

                if (_fallDistance > 0f) {
                    Entity.OnFallOnBlock(new EntityFallOnBlockTraitEvent(Entity.Position, _fallDistance));
                }

                Entity.Velocity = new Vec3f {
                    X = groundedVelocityX,
                    Y = 0f,
                    Z = groundedVelocityZ
                };
                _fallDistance = 0f;
                break;
            }

            if (Entity.Velocity.Y < 0f) {
                _fallDistance += -Entity.Velocity.Y;
            }
            else {
                _fallDistance = 0f;
            }

            Entity.Position = new Vec3f {
                X = nextX,
                Y = nextY,
                Z = nextZ
            };
            Entity.Velocity = new Vec3f {
                X = velocityX,
                Y = Entity.Velocity.Y,
                Z = velocityZ
            };
        }

        if (previousPosition.X == Entity.Position.X &&
            previousPosition.Y == Entity.Position.Y &&
            previousPosition.Z == Entity.Position.Z) {
            Entity.OnPhysicsTick(details.CurrentTick, IsGrounded(Entity.Position.X, Entity.Position.Y, Entity.Position.Z));
            return;
        }

        OnMove(new EntityMoveOptions(
            previousPosition,
            Entity.Position,
            new MovementRotation(),
            new MovementRotation()));

        Entity.OnPhysicsTick(details.CurrentTick, IsGrounded(Entity.Position.X, Entity.Position.Y, Entity.Position.Z));
    }

    // public override void OnSpawn(EntitySpawnOptions details) {}

    // public override void OnRemove() {}

    // public override void OnInteract(Core.Player player, EntityInteractMethod method) {}


    public override void OnMove(EntityMoveOptions details) {
        base.OnMove(details);

        if (Entity.Dimension is null) {
            return;
        }

        Entity.Dimension.Broadcast(new MoveActorDeltaPacket() {
            EntityRuntimeId = Entity.RuntimeId,
            Flags = (ushort)MoveDeltaFlags.All,
            Position = details.To,
            Rotation = new Vec3f() {
                X = details.ToRotation.Pitch,
                Y = details.ToRotation.Yaw,
                Z = details.ToRotation.HeadYaw,
            }
        });
    }



    public override EntityTrait Clone(Entity entity) {
        return new EntityMovementTrait(entity) {
            Speed = Speed,
            GravityPerTick = GravityPerTick,
            Drag = Drag,
            TerminalVelocity = TerminalVelocity,
            GroundFriction = GroundFriction,
            MinHorizontalVelocity = MinHorizontalVelocity,
            WaterCurrentForce = WaterCurrentForce
        };
    }

    public void SetAttribute(AttributeName name, float current, float @default) {
        const float min = 0f;
        const float max = float.MaxValue;

        Protocol.Types.Attribute attribute = Entity.Attributes.GetAttribute(name)
            ?? new Protocol.Types.Attribute(min, max, current, @default, name);

        attribute.Min = min;
        attribute.Max = max;
        attribute.DefaultMin = min;
        attribute.DefaultMax = max;
        attribute.Default = @default;
        attribute.Current = current;
        Entity.Attributes.SetAttribute(attribute);
    }

    private bool IsGrounded(float x, float y, float z) {
        if (Entity.Dimension is null) {
            return false;
        }

        return HasSolidBelow(
            x,
            y - 0.001f,
            z
        );
    }

    private bool HasSolidBelow(float x, float y, float z) {
        float halfWidth = CollisionWidth() * 0.5f;
        int minX = (int)MathF.Floor(x - halfWidth + CollisionEpsilon);
        int maxX = (int)MathF.Floor(x + halfWidth - CollisionEpsilon);
        int blockY = (int)MathF.Floor(y);
        int minZ = (int)MathF.Floor(z - halfWidth + CollisionEpsilon);
        int maxZ = (int)MathF.Floor(z + halfWidth - CollisionEpsilon);

        for (int blockX = minX; blockX <= maxX; blockX++) {
            for (int blockZ = minZ; blockZ <= maxZ; blockZ++) {
                if (IsSolid(blockX, blockY, blockZ)) {
                    return true;
                }
            }
        }

        return false;
    }

    private bool CollidesWithSolidBlocks(float x, float y, float z) {
        float halfWidth = CollisionWidth() * 0.5f;
        int minX = (int)MathF.Floor(x - halfWidth + CollisionEpsilon);
        int maxX = (int)MathF.Floor(x + halfWidth - CollisionEpsilon);
        int minY = (int)MathF.Floor(y + CollisionEpsilon);
        int maxY = (int)MathF.Floor(y + CollisionHeight() - CollisionEpsilon);
        int minZ = (int)MathF.Floor(z - halfWidth + CollisionEpsilon);
        int maxZ = (int)MathF.Floor(z + halfWidth - CollisionEpsilon);

        for (int blockX = minX; blockX <= maxX; blockX++) {
            for (int blockY = minY; blockY <= maxY; blockY++) {
                for (int blockZ = minZ; blockZ <= maxZ; blockZ++) {
                    if (IsSolid(blockX, blockY, blockZ)) {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool IsSolid(int x, int y, int z) {
        if (Entity.Dimension is null) {
            return false;
        }

        var type = Entity.Dimension.GetPermutation(
            x,
            y,
            z
        ).Type;

        return type.Solid && !type.Air && !type.Liquid;
    }

    private float CollisionWidth() {
        return Entity.GetTrait<EntityCollisionTrait>()?.Width ?? EntityCollisionTrait.DefaultWidth;
    }

    private float CollisionHeight() {
        return Entity.GetTrait<EntityCollisionTrait>()?.Height ?? EntityCollisionTrait.DefaultHeight;
    }

    private float FindGroundSurface(float x, float fromY, float toY, float z) {
        int startBlockY = (int)MathF.Floor(fromY - CollisionEpsilon);
        int endBlockY = (int)MathF.Floor(toY - CollisionEpsilon);

        float halfWidth = CollisionWidth() * 0.5f;
        int minX = (int)MathF.Floor(x - halfWidth + CollisionEpsilon);
        int maxX = (int)MathF.Floor(x + halfWidth - CollisionEpsilon);
        int minZ = (int)MathF.Floor(z - halfWidth + CollisionEpsilon);
        int maxZ = (int)MathF.Floor(z + halfWidth - CollisionEpsilon);

        for (int blockY = startBlockY; blockY >= endBlockY; blockY--) {
            bool solid = false;
            for (int bx = minX; bx <= maxX && !solid; bx++) {
                for (int bz = minZ; bz <= maxZ && !solid; bz++) {
                    if (IsSolid(bx, blockY, bz)) {
                        solid = true;
                    }
                }
            }

            if (solid) {
                return blockY + 1f;
            }
        }

        int groundY = (int)MathF.Floor(toY - CollisionEpsilon);
        return groundY + 1f;
    }
}






