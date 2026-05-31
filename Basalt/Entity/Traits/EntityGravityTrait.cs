namespace Basalt.Server.Entity.Traits;

using Basalt.Server.Entity.Traits.Types;
using Basalt.Protocol.Enums;
using Basalt.Server.Traits;


public sealed class EntityGravityTrait : EntityTrait
{
    public new static string Identifier => "gravity";
    public new static readonly string[] Components = ["minecraft:movement.basic", "minecraft:movement.jump", "minecraft:movement"];

    private float _verticalVelocity;
    private float _fallDistance;

    public float GravityPerTick { get; set; } = 0.08f;
    public float Drag { get; set; } = 0.98f;
    public float TerminalVelocity { get; set; } = -3.92f;

    public EntityGravityTrait(Entity entity) : base(entity)
    {
    }

    public override void OnSpawn(EntitySpawnOptions details)
    {
        if (!Entity.Flags.GetActorFlag(ActorFlag.HasGravity))
        {
            Entity.Flags.SetActorFlag(ActorFlag.HasGravity, true);
        }

        _verticalVelocity = 0f;
        _fallDistance = 0f;
    }

    public override void OnTick(TraitOnTickDetails details)
    {
        if (!Entity.IsAlive || Entity.Dimension is null || Entity.IsPlayer())
        {
            return;
        }

        if (!Entity.Flags.GetActorFlag(ActorFlag.HasGravity) || Entity.IsSwimming)
        {
            _verticalVelocity = 0f;
            _fallDistance = 0f;
            return;
        }

        if (IsGrounded(Entity.Position.Y))
        {
            if (_verticalVelocity < 0f && _fallDistance > 0f)
            {
                Entity.OnFallOnBlock(new EntityFallOnBlockTraitEvent(Entity.Position, _fallDistance));
            }

            _verticalVelocity = 0f;
            _fallDistance = 0f;
            return;
        }

        for (uint i = 0; i < details.DeltaTick; i++)
        {
            _verticalVelocity -= GravityPerTick;
            _verticalVelocity *= Drag;
            if (_verticalVelocity < TerminalVelocity)
            {
                _verticalVelocity = TerminalVelocity;
            }

            float nextY = Entity.Position.Y + _verticalVelocity;
            if (IsGrounded(nextY))
            {
                int groundY = (int)MathF.Floor(nextY - 0.001f);
                Entity.Position = new Basalt.Protocol.Types.Vec3f
                {
                    X = Entity.Position.X,
                    Y = groundY + 1f,
                    Z = Entity.Position.Z
                };

                if (_fallDistance > 0f)
                {
                    Entity.OnFallOnBlock(new EntityFallOnBlockTraitEvent(Entity.Position, _fallDistance));
                }

                _verticalVelocity = 0f;
                _fallDistance = 0f;
                return;
            }

            float fallStep = Entity.Position.Y - nextY;
            if (fallStep > 0f)
            {
                _fallDistance += fallStep;
            }

            Entity.Position = new Basalt.Protocol.Types.Vec3f
            {
                X = Entity.Position.X,
                Y = nextY,
                Z = Entity.Position.Z
            };
        }
    }

    public override EntityTrait Clone(Entity entity)
    {
        return new EntityGravityTrait(entity)
        {
            GravityPerTick = GravityPerTick,
            Drag = Drag,
            TerminalVelocity = TerminalVelocity
        };
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






