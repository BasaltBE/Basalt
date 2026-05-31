namespace Basalt.Server.Entity.Traits;

using Basalt.Server.Entity.Traits.Types;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.Server.Traits;

public sealed class EntityMovementTrait : EntityTrait
{
    public new static string Identifier => "movement";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player, EntityIdentifier.Item];
    public new static readonly string[] Components = ["minecraft:movement", "minecraft:movement.basic", "minecraft:movement.jump"];

    public float BaseMovementSpeed => 0.1f;
    public float BaseUnderwaterMovementSpeed => 0.02f;
    public float BaseLavaMovementSpeed => 0.02f;


    public float Speed { get; private set; } = 1f;
    private float _fallDistance;
    public float GravityPerTick { get; set; } = 0.08f;
    public float Drag { get; set; } = 0.98f;
    public float TerminalVelocity { get; set; } = -3.92f;
    public float GroundFriction { get; set; } = 0.6f;
    public float MinHorizontalVelocity { get; set; } = 0.01f;


    public EntityMovementTrait(Entity entity) : base(entity)
    { }



    // public override void OnTick(TraitOnTickDetails details) {}

    public void SetSpeed(float speed = 1f)
    {
        Speed = speed;

        float movement = BaseMovementSpeed * Speed;
        float underwater = BaseUnderwaterMovementSpeed * Speed;
        float lava = BaseLavaMovementSpeed * Speed;

        SetAttribute(AttributeName.Movement, movement, BaseMovementSpeed);
        SetAttribute(AttributeName.UnderwaterMovement, underwater, BaseUnderwaterMovementSpeed);
        SetAttribute(AttributeName.LavaMovement, lava, BaseLavaMovementSpeed);
    }

    public override void OnAdd()
    {
        SetSpeed(Speed);
    }

    public override void OnSpawn(EntitySpawnOptions details)
    {
        if (!Entity.Flags.GetActorFlag(ActorFlag.HasGravity))
        {
            Entity.Flags.SetActorFlag(ActorFlag.HasGravity, true);
        }

        _fallDistance = 0f;
    }

    public override void OnTick(TraitOnTickDetails details)
    {
        if (!Entity.IsAlive || Entity.Dimension is null || Entity.IsPlayer())
        {
            return;
        }

        Vec3f previousPosition = Entity.Position;

        for (uint i = 0; i < details.DeltaTick; i++)
        {
            bool applyGravity = Entity.Flags.GetActorFlag(ActorFlag.HasGravity) && !Entity.IsSwimming;
            if (applyGravity)
            {
                Entity.Velocity = new Vec3f
                {
                    X = Entity.Velocity.X,
                    Y = Entity.Velocity.Y - GravityPerTick,
                    Z = Entity.Velocity.Z
                };
                Entity.Velocity = new Vec3f
                {
                    X = Entity.Velocity.X,
                    Y = Entity.Velocity.Y * Drag,
                    Z = Entity.Velocity.Z
                };
                if (Entity.Velocity.Y < TerminalVelocity)
                {
                    Entity.Velocity = new Vec3f
                    {
                        X = Entity.Velocity.X,
                        Y = TerminalVelocity,
                        Z = Entity.Velocity.Z
                    };
                }
            }

            float nextX = Entity.Position.X + Entity.Velocity.X;
            float nextY = Entity.Position.Y + Entity.Velocity.Y;
            float nextZ = Entity.Position.Z + Entity.Velocity.Z;

            if (applyGravity && Entity.Velocity.Y <= 0f && IsGrounded(nextY))
            {
                int groundY = (int)MathF.Floor(nextY - 0.001f);
                float groundedVelocityX = Entity.Velocity.X * GroundFriction;
                float groundedVelocityZ = Entity.Velocity.Z * GroundFriction;
                if (MathF.Abs(groundedVelocityX) < MinHorizontalVelocity)
                {
                    groundedVelocityX = 0f;
                }
                if (MathF.Abs(groundedVelocityZ) < MinHorizontalVelocity)
                {
                    groundedVelocityZ = 0f;
                }

                Entity.Position = new Vec3f
                {
                    X = nextX,
                    Y = groundY + 1f,
                    Z = nextZ
                };

                if (_fallDistance > 0f)
                {
                    Entity.OnFallOnBlock(new EntityFallOnBlockTraitEvent(Entity.Position, _fallDistance));
                }

                Entity.Velocity = new Vec3f
                {
                    X = groundedVelocityX,
                    Y = 0f,
                    Z = groundedVelocityZ
                };
                _fallDistance = 0f;
                break;
            }

            if (Entity.Velocity.Y < 0f)
            {
                _fallDistance += -Entity.Velocity.Y;
            }
            else
            {
                _fallDistance = 0f;
            }

            Entity.Position = new Vec3f
            {
                X = nextX,
                Y = nextY,
                Z = nextZ
            };
        }

        if (previousPosition.X == Entity.Position.X &&
            previousPosition.Y == Entity.Position.Y &&
            previousPosition.Z == Entity.Position.Z)
        {
            Entity.OnPhysicsTick(details.CurrentTick, IsGrounded(Entity.Position.Y));
            return;
        }

        OnMove(new EntityMoveOptions(
            previousPosition,
            Entity.Position,
            new MovementRotation(),
            new MovementRotation()));

        Entity.OnPhysicsTick(details.CurrentTick, IsGrounded(Entity.Position.Y));
    }

    // public override void OnSpawn(EntitySpawnOptions details) {}

    // public override void OnRemove() {}

    // public override void OnInteract(Core.Player player, EntityInteractMethod method) {}


    public override void OnMove(EntityMoveOptions details)
    {
        base.OnMove(details);

        var update = new MoveActorDeltaPacket()
        {
            EntityRuntimeId = Entity.RuntimeId,
            Flags = (ushort)MoveDeltaFlags.All,
            Position = details.To,
            Rotation = new Vec3f()
            {
                X = details.ToRotation.Pitch,
                Y = details.ToRotation.Yaw,
                Z = details.ToRotation.HeadYaw,
            }
        };

        if (Entity.Dimension is not null)
            Entity.Dimension.Broadcast(update);
    }



    public override EntityTrait Clone(Entity entity)
    {
        return new EntityMovementTrait(entity)
        {
            Speed = Speed,
            GravityPerTick = GravityPerTick,
            Drag = Drag,
            TerminalVelocity = TerminalVelocity,
            GroundFriction = GroundFriction,
            MinHorizontalVelocity = MinHorizontalVelocity
        };
    }

    public void SetAttribute(AttributeName name, float current, float @default)
    {
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

    private bool IsGrounded(float y)
    {
        if (Entity.Dimension is null)
        {
            return false;
        }

        string identifier = Entity.Dimension.GetPermutation(
            (int)MathF.Floor(Entity.Position.X),
            (int)MathF.Floor(y - 0.001f),
            (int)MathF.Floor(Entity.Position.Z)
        ).Type.Identifier;

        if (string.Equals(identifier, "minecraft:air", StringComparison.Ordinal))
        {
            return false;
        }

        if (identifier.Contains("water", StringComparison.Ordinal) || identifier.Contains("lava", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }
}






