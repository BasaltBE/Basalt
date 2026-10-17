using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class SummonCommand : Command
{
    public SummonCommand() : base("summon", "Summon an entity.", "basalt.command.summon")
    {
        AddOverload(
            new CommandParameter("entity", new EntityEnum()),
            new CommandParameter("position", new PositionEnum(), optional: true)
        );
    }

    public override CommandResult Execute(CommandContext context)
    {
        EntityType type = context.Get<EntityType>("entity");
        if (type.Summonable == false)
            return new(false, $"Entity '{type.Identifier}' cannot be summoned.");

        Dimension? dimension = context.Player?.Dimension;
        float x = 0.5f;
        float y = -60;
        float z = 0.5f;
        if (context.Arguments[1] is Vec3 position)
        {
            x = position.X;
            y = position.Y;
            z = position.Z;
        }
        else if (context.Player is { } player)
        {
            x = player.Position.X + MathF.Cos(player.Rotation.Y * (MathF.PI / 180)) * 2;
            y = player.Position.Y;
            z = player.Position.Z + MathF.Sin(player.Rotation.Y * (MathF.PI / 180)) * 2;
        }
        else
        {
            foreach (Dimension candidate in context.Server.GetWorld().Dimensions.Values)
            {
                dimension = candidate;
                break;
            }
        }

        if (dimension is null)
            return new(false, "No dimension is available to summon the entity.");

        Entity entity = new(type);
        entity.Position.X = x;
        entity.Position.Y = y;
        entity.Position.Z = z;
        entity.Spawn(dimension);
        return new(true, $"Summoned {type.Identifier}.");
    }
}
