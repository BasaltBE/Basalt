using Basalt.Containers;
using Basalt.Entity.Container;
using Basalt.Entity.Traits.Enums;
using Basalt.Entity.Traits.Types;
using Basalt.Item;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;
using Basalt.Traits;

namespace Basalt.Entity.Traits;

public sealed class EntityMovementTrait : EntityTrait
{
    public new static string Identifier => "movement";
    public new static readonly EntityIdentifier[] Types = [EntityIdentifier.Player];
    public new static readonly string[] Components = ["minecraft:movement"];

    public float BaseMovementSpeed => 0.1f;
    public float BaseUnderwaterMovementSpeed => 0.02f;
    public float BaseLavaMovementSpeed => 0.02f;


    public float Speed { get; private set; } = 1f;


    public EntityMovementTrait(Entity entity) : base(entity)
    { }



    // public override void OnTick(TraitOnTickDetails details) {}

    public void SetSpeed(float speed = 1f)
    {
        Logger.Info("Setting speed to {0}", speed);

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
}
