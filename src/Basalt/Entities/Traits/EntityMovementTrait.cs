namespace Basalt.Core.Entities.Traits;

using Basalt.Core.Blocks.Traits;
using Basalt.Core.Blocks.Types;
using Basalt.Core.Blocks;
using Basalt.Core.Blocks.Components;
using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Profiling;
using Basalt.Core.Traits;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Player;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;
using System.Text.Json;

public sealed class EntityMovementTrait : EntityTrait {
    public new static string Identifier => "movement";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player, EntityIdentifier.Item];
    public new static readonly string[] Components = ["minecraft:movement", "minecraft:movement.basic", "minecraft:movement.jump"];

    public static float BaseMovementSpeed => 0.1f;
    public static float BaseUnderwaterMovementSpeed => 0.02f;
    public static float BaseLavaMovementSpeed => 0.02f;


    public float Speed { get; private set; } = 1f;
    public float MovementSpeed => BaseMovementSpeed * Speed;
    public float AiMovementSpeed => MovementSpeed; // 0.8796f;
    private float _fallDistance;
    private float _tickCollisionWidth = EntityCollisionTrait.DefaultWidth;
    private float _tickCollisionHeight = EntityCollisionTrait.DefaultHeight;
    public float GravityPerTick { get; set; } = 0.08f;
    public float Drag { get; set; } = 0.98f;
    public bool DragBeforeGravity;
    public float TerminalVelocity { get; set; } = -3.92f;
    public float GroundFriction { get; set; } = 0.6f;
    public float MinHorizontalVelocity { get; set; } = 0.01f;
    public float WaterCurrentForce { get; set; } = 0.08f;
    public bool Grounded { get; private set; }
    public bool InLava { get; private set; }
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
        if (Entity is ItemEntity) {
            GravityPerTick = 0.04f;
            Drag = 0.98f;
            DragBeforeGravity = true;
        }

        float movementSpeed = BaseMovementSpeed;
        if (Entity.Type.TryGetComponentProperties("minecraft:movement", out JsonElement properties) &&
            properties.TryGetProperty("value", out JsonElement value)) {
            if (value.ValueKind == JsonValueKind.Number) {
                value.TryGetSingle(out movementSpeed);
            }
            else if (value.ValueKind == JsonValueKind.String) {
                if (float.TryParse(value.GetString(), out float parsed)) {
                    movementSpeed = parsed;
                }
            }
        }

        SetSpeed(movementSpeed > 0f ? movementSpeed / BaseMovementSpeed : Speed);
    }

    public override void OnSpawn(EntitySpawnOptions details) {
        if (Entity.Type.HasGravity != false && !Entity.Flags.GetActorFlag(Entities.ActorFlag.HasGravity)) {
            Entity.Flags.SetActorFlag(Entities.ActorFlag.HasGravity, true);
        }

        _fallDistance = 0f;
    }

    public override void OnTick(TraitOnTickDetails details) {
        if (!Entity.IsAlive || Entity.Dimension is null || Entity.IsPlayer()) {
            return;
        }

        using var __zone = Profiler.Enabled ? Profiler.BeginZone("EntityMovement.OnTick") : default;
        EntityCollisionTrait? collision = Entity.GetTrait<EntityCollisionTrait>();
        _tickCollisionWidth = collision?.Width ?? EntityCollisionTrait.DefaultWidth;
        _tickCollisionHeight = collision?.Height ?? EntityCollisionTrait.DefaultHeight;
        UpdateFluidState(Entity.Position);
        Vec3 previousPosition = Entity.Position;
        if (collision is not null) {
            collision.XAxisCollision = 0;
            collision.YAxisCollision = 0;
            collision.ZAxisCollision = 0;
        }

        for (uint i = 0; i < details.DeltaTick; i++) {
            float flowX = 0f;
            float flowY = 0f;
            float flowZ = 0f;
            float velocityX = Entity.Velocity.X;
            float velocityY = Entity.Velocity.Y;
            float velocityZ = Entity.Velocity.Z;
            float halfWidth = _tickCollisionWidth * 0.5f;
            int minWaterX = (int)MathF.Floor(Entity.Position.X - halfWidth + CollisionEpsilon);
            int maxWaterX = (int)MathF.Floor(Entity.Position.X + halfWidth - CollisionEpsilon);
            int minWaterY = (int)MathF.Floor(Entity.Position.Y + CollisionEpsilon);
            int maxWaterY = (int)MathF.Floor(Entity.Position.Y + _tickCollisionHeight - CollisionEpsilon);
            int minWaterZ = (int)MathF.Floor(Entity.Position.Z - halfWidth + CollisionEpsilon);
            int maxWaterZ = (int)MathF.Floor(Entity.Position.Z + halfWidth - CollisionEpsilon);

            for (int waterX = minWaterX; waterX <= maxWaterX; waterX++) {
                for (int waterY = minWaterY; waterY <= maxWaterY; waterY++) {
                    for (int waterZ = minWaterZ; waterZ <= maxWaterZ; waterZ++) {
                        FluidTrait.GetWaterFlow(
                            Entity.Dimension,
                            new BlockPos { X = waterX, Y = waterY, Z = waterZ },
                            out float flowBlockX,
                            out float flowBlockY,
                            out float flowBlockZ,
                            out float waterHeight);

                        if (waterHeight == 0f || Entity.Position.Y >= waterY + waterHeight) {
                            continue;
                        }

                        flowX += flowBlockX;
                        flowY += flowBlockY;
                        flowZ += flowBlockZ;
                    }
                }
            }

            float flowLength = MathF.Sqrt((flowX * flowX) + (flowY * flowY) + (flowZ * flowZ));
            if (flowLength > 0f) {
                velocityX += flowX / flowLength * WaterCurrentForce;
                velocityY += flowY / flowLength * WaterCurrentForce;
                velocityZ += flowZ / flowLength * WaterCurrentForce;
            }

            bool applyGravity = Entity.Flags.GetActorFlag(ActorFlag.HasGravity) && !Entity.IsSwimming;
            if (applyGravity) {
                float gravity = InLava
                    ? GravityPerTick * 0.25f
                    : Entity.IsInWater ? GravityPerTick * 0.25f : GravityPerTick;
                float fluidDrag = InLava ? 0.5f : Entity.IsInWater ? 0.8f : Drag;
                if (DragBeforeGravity) {
                    velocityY *= fluidDrag;
                }

                velocityY -= gravity;
                if (!DragBeforeGravity) {
                    velocityY *= fluidDrag;
                }
                velocityX *= fluidDrag;
                velocityZ *= fluidDrag;
                velocityY = MathF.Max(velocityY, TerminalVelocity);
            }

            float nextX = Entity.Position.X + velocityX;
            float nextY = Entity.Position.Y + velocityY;
            float nextZ = Entity.Position.Z + velocityZ;
            float horizontalCollisionY = velocityY > 0f ? nextY : Entity.Position.Y;
            if (velocityX != 0f && CollidesWithSolidBlocks(nextX, horizontalCollisionY, Entity.Position.Z)) {
                if (collision is not null) {
                    collision.XAxisCollision = velocityX > 0f ? 1 : -1;
                }

                nextX = Entity.Position.X;
                velocityX = 0f;
            }

            if (velocityZ != 0f && CollidesWithSolidBlocks(nextX, horizontalCollisionY, nextZ)) {
                if (collision is not null) {
                    collision.ZAxisCollision = velocityZ > 0f ? 1 : -1;
                }

                nextZ = Entity.Position.Z;
                velocityZ = 0f;
            }

            if (applyGravity && velocityY <= 0f &&
                FindGroundSurface(nextX, Entity.Position.Y, nextY, nextZ) is float landingY) {
                if (collision is not null) {
                    collision.YAxisCollision = -1;
                }

                float groundFriction = SurfaceFriction(nextX, landingY, nextZ);
                float groundedVelocityX = velocityX * groundFriction;
                float groundedVelocityZ = velocityZ * groundFriction;
                if (MathF.Abs(groundedVelocityX) < MinHorizontalVelocity) {
                    groundedVelocityX = 0f;
                }
                if (MathF.Abs(groundedVelocityZ) < MinHorizontalVelocity) {
                    groundedVelocityZ = 0f;
                }

                Entity.Position = new Vec3 {
                    X = nextX,
                    Y = landingY,
                    Z = nextZ
                };

                if (_fallDistance > 0f) {
                    Entity.OnFallOnBlock(new EntityFallOnBlockTraitEvent(Entity.Position, _fallDistance));
                }

                Entity.Velocity = new Vec3 { X = groundedVelocityX, Y = 0f, Z = groundedVelocityZ };
                _fallDistance = 0f;
                break;
            }

            if (velocityY < 0f) {
                _fallDistance += -velocityY;
            }
            else {
                _fallDistance = 0f;
            }

            Entity.Position = new Vec3 {
                X = nextX,
                Y = nextY,
                Z = nextZ
            };
            Entity.Velocity = new Vec3 { X = velocityX, Y = velocityY, Z = velocityZ };
        }

        if (previousPosition.X == Entity.Position.X &&
            previousPosition.Y == Entity.Position.Y &&
            previousPosition.Z == Entity.Position.Z) {
            Grounded = !InLava && IsGrounded(Entity.Position.X, Entity.Position.Y, Entity.Position.Z);
            Entity.OnPhysicsTick(details.CurrentTick, Grounded);
            return;
        }

        Entity.OnMove(new EntityMoveOptions(
            previousPosition,
            Entity.Position,
            new MovementRotation {
                Pitch = Entity.Rotation.X,
                Yaw = Entity.Rotation.Y,
                HeadYaw = Entity.Rotation.Z
            },
            new MovementRotation {
                Pitch = Entity.Rotation.X,
                Yaw = Entity.Rotation.Y,
                HeadYaw = Entity.Rotation.Z
            }));

        UpdateFluidState(Entity.Position);
        Grounded = !InLava && IsGrounded(Entity.Position.X, Entity.Position.Y, Entity.Position.Z);
        Entity.OnPhysicsTick(details.CurrentTick, Grounded);
    }

    // public override void OnSpawn(EntitySpawnOptions details) {}

    // public override void OnRemove() {}

    // public override void OnInteract(Core.Player player, EntityInteractMethod method) {}


    public override void OnMove(EntityMoveOptions details) {
        base.OnMove(details);

        if (Entity.Dimension is null) {
            return;
        }

        Vec3 networkPosition = Entity is Player player
            ? player.GetEyePosition()
            : details.To;

        Entity.Dimension.Broadcast(new MoveActorAbsolutePacket {
            ActorRuntimeId = Entity.RuntimeId,
            Header = 0,
            Position = networkPosition,
            RotationX = unchecked((byte)PackRotation(details.ToRotation.Pitch)),
            RotationY = unchecked((byte)PackRotation(details.ToRotation.Yaw)),
            RotationYHead = unchecked((byte)PackRotation(details.ToRotation.HeadYaw))
        }, new BroadcastOptions {
            Except = Entity.IsPlayer() ? [Entity] : null
        });
    }



    private static sbyte PackRotation(float degrees) {
        int packed = (int)(degrees * (256f / 360f));
        return unchecked((sbyte)packed);
    }


    public override EntityTrait Clone(Entity entity) {
        return new EntityMovementTrait(entity) {
            Speed = Speed,
            GravityPerTick = GravityPerTick,
            Drag = Drag,
            DragBeforeGravity = DragBeforeGravity,
            TerminalVelocity = TerminalVelocity,
            GroundFriction = GroundFriction,
            MinHorizontalVelocity = MinHorizontalVelocity,
            WaterCurrentForce = WaterCurrentForce
        };
    }

    public void SetAttribute(AttributeName name, float current, float @default) {
        const float min = 0f;
        const float max = float.MaxValue;

        AttributeData attribute = Entity.Attributes.GetAttribute(name)
            ?? new AttributeData {
                Current = current,
                DefaultMaximum = max,
                DefaultMinimum = min,
                Default = @default,
                Maximum = max,
                Minimum = min,
                Modifiers = [],
                Name = name.ToProtocolString(),
            }; // (min, max, current, @default, name);

        attribute.Minimum = min;
        attribute.Maximum = max;
        attribute.DefaultMinimum = min;
        attribute.DefaultMaximum = max;
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
        float halfWidth = _tickCollisionWidth * 0.5f;
        int minX = (int)MathF.Floor(x - halfWidth + CollisionEpsilon);
        int maxX = (int)MathF.Floor(x + halfWidth - CollisionEpsilon);
        int blockY = (int)MathF.Floor(y);
        int minZ = (int)MathF.Floor(z - halfWidth + CollisionEpsilon);
        int maxZ = (int)MathF.Floor(z + halfWidth - CollisionEpsilon);

        for (int blockX = minX; blockX <= maxX; blockX++) {
            for (int blockZ = minZ; blockZ <= maxZ; blockZ++) {
                foreach (CollisionBox box in GetCollisionBoxes(blockX, blockY, blockZ)) {
                    float top = blockY + (box.OriginY + box.SizeY) / 16f;
                    if (MathF.Abs(y - top) <= 0.01f &&
                        OverlapsHorizontal(x, z, _tickCollisionWidth, blockX, blockZ, box)) {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool CollidesWithSolidBlocks(float x, float y, float z) {
        float halfWidth = _tickCollisionWidth * 0.5f;
        int minX = (int)MathF.Floor(x - halfWidth + CollisionEpsilon);
        int maxX = (int)MathF.Floor(x + halfWidth - CollisionEpsilon);
        int minY = (int)MathF.Floor(y + CollisionEpsilon);
        int maxY = (int)MathF.Floor(y + _tickCollisionHeight - CollisionEpsilon);
        int minZ = (int)MathF.Floor(z - halfWidth + CollisionEpsilon);
        int maxZ = (int)MathF.Floor(z + halfWidth - CollisionEpsilon);

        for (int blockX = minX; blockX <= maxX; blockX++) {
            for (int blockY = minY; blockY <= maxY; blockY++) {
                for (int blockZ = minZ; blockZ <= maxZ; blockZ++) {
                    foreach (CollisionBox box in GetCollisionBoxes(blockX, blockY, blockZ)) {
                        if (Overlaps(
                            x - halfWidth,
                            y,
                            z - halfWidth,
                            x + halfWidth,
                            y + _tickCollisionHeight,
                            z + halfWidth,
                            blockX,
                            blockY,
                            blockZ,
                            box)) {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private CollisionBox[] GetCollisionBoxes(int x, int y, int z) {
        if (Entity.Dimension is null) {
            return [];
        }

        return Entity.Dimension.TryGetLoadedPermutation(x, y, z, out BlockPermutation? permutation) &&
            permutation is not null
            ? BlockCollisionShape.GetBoxArray(permutation)
            : [];
    }

    private void UpdateFluidState(Vec3 position) {
        if (Entity.Dimension is null) {
            Entity.IsInWater = false;
            InLava = false;
            return;
        }

        bool inWater = false;
        bool inLava = false;
        float halfWidth = _tickCollisionWidth * 0.5f;
        int minX = (int)MathF.Floor(position.X - halfWidth + CollisionEpsilon);
        int maxX = (int)MathF.Floor(position.X + halfWidth - CollisionEpsilon);
        int minY = (int)MathF.Floor(position.Y + CollisionEpsilon);
        int maxY = (int)MathF.Floor(position.Y + _tickCollisionHeight - CollisionEpsilon);
        int minZ = (int)MathF.Floor(position.Z - halfWidth + CollisionEpsilon);
        int maxZ = (int)MathF.Floor(position.Z + halfWidth - CollisionEpsilon);

        for (int x = minX; x <= maxX; x++) {
            for (int y = minY; y <= maxY; y++) {
                for (int z = minZ; z <= maxZ; z++) {
                    if (!Entity.Dimension.TryGetLoadedPermutation(x, y, z, out BlockPermutation? permutation) ||
                        permutation is null) {
                        continue;
                    }

                    if (permutation.Type.Water) {
                        inWater = true;
                    }
                    else if (permutation.Type.Lava) {
                        inLava = true;
                    }

                    if (inWater && inLava) {
                        Entity.IsInWater = true;
                        InLava = true;
                        return;
                    }
                }
            }
        }

        Entity.IsInWater = inWater;
        InLava = inLava;
    }

    private float SurfaceFriction(float x, float y, float z) {
        if (Entity.Dimension is null) {
            return GroundFriction;
        }

        int blockX = (int)MathF.Floor(x);
        int blockY = (int)MathF.Floor(y - CollisionEpsilon);
        int blockZ = (int)MathF.Floor(z);
        if (!Entity.Dimension.TryGetLoadedPermutation(blockX, blockY, blockZ, out BlockPermutation? permutation) ||
            permutation is null) {
            return GroundFriction;
        }

        BlockType type = permutation.Type;
        if (type.Liquid || type.Air) {
            return GroundFriction;
        }

        return Math.Clamp(type.Friction, GroundFriction, 0.99f);
    }

    private static bool OverlapsHorizontal(
        float entityX,
        float entityZ,
        float entityWidth,
        int blockX,
        int blockZ,
        CollisionBox box) {
        float halfWidth = entityWidth * 0.5f;
        return entityX + halfWidth > blockX + (box.OriginX + 8f) / 16f &&
            entityX - halfWidth < blockX + (box.OriginX + box.SizeX + 8f) / 16f &&
            entityZ + halfWidth > blockZ + (box.OriginZ + 8f) / 16f &&
            entityZ - halfWidth < blockZ + (box.OriginZ + box.SizeZ + 8f) / 16f;
    }

    private static bool Overlaps(
        float minX,
        float minY,
        float minZ,
        float maxX,
        float maxY,
        float maxZ,
        int blockX,
        int blockY,
        int blockZ,
        CollisionBox box) {
        float boxMinX = blockX + (box.OriginX + 8f) / 16f;
        float boxMinY = blockY + box.OriginY / 16f;
        float boxMinZ = blockZ + (box.OriginZ + 8f) / 16f;
        float boxMaxX = boxMinX + box.SizeX / 16f;
        float boxMaxY = boxMinY + box.SizeY / 16f;
        float boxMaxZ = boxMinZ + box.SizeZ / 16f;
        return maxX > boxMinX && minX < boxMaxX &&
            maxY > boxMinY && minY < boxMaxY &&
            maxZ > boxMinZ && minZ < boxMaxZ;
    }

    private float? FindGroundSurface(float x, float fromY, float toY, float z) {
        int startBlockY = (int)MathF.Floor(fromY + CollisionEpsilon);
        int endBlockY = (int)MathF.Floor(toY - CollisionEpsilon);

        float halfWidth = _tickCollisionWidth * 0.5f;
        int minX = (int)MathF.Floor(x - halfWidth + CollisionEpsilon);
        int maxX = (int)MathF.Floor(x + halfWidth - CollisionEpsilon);
        int minZ = (int)MathF.Floor(z - halfWidth + CollisionEpsilon);
        int maxZ = (int)MathF.Floor(z + halfWidth - CollisionEpsilon);

        for (int blockY = startBlockY; blockY >= endBlockY; blockY--) {
            bool solid = false;
            for (int bx = minX; bx <= maxX && !solid; bx++) {
                for (int bz = minZ; bz <= maxZ && !solid; bz++) {
                    foreach (CollisionBox box in GetCollisionBoxes(bx, blockY, bz)) {
                        float top = blockY + (box.OriginY + box.SizeY) / 16f;
                        if (fromY <= top + CollisionEpsilon && toY <= top + CollisionEpsilon &&
                            OverlapsHorizontal(x, z, _tickCollisionWidth, bx, bz, box)) {
                            solid = true;
                            break;
                        }
                    }
                }
            }

            if (solid) {
                return blockY + 1f;
            }
        }

        return null;
    }
}
