namespace Basalt.Server.Commands.List.Operator;

using Basalt.Protocol.Enums;
using Basalt.Server.Commands;
using Basalt.Server.Entity.Traits.Types;
using Basalt.Server.World.Dimension;
using Player = global::Basalt.Server.Player.Player;
using Vec3f = Basalt.Protocol.Types.Vec3f;

public sealed class SummonCommand : Command
{
    public SummonCommand() : base("summon", "Summon an entity")
    {
        Permissions.Add("basalt.op");

        CreateOverload()
            .Set<EntityEnum>("entity", true)
            .Set<PositionEnum>("position", false);
    }

    public override CommandResult Execute(CommandExecutionState state)
    {
        EntityEnum? entityArg = state.Get<EntityEnum>("entity");
        if (string.IsNullOrWhiteSpace(entityArg?.EntityIdentifier))
        {
            return CommandResult.Empty(false);
        }

        string identifier = entityArg.EntityIdentifier;

        Dimension? dimension = null;
        Vec3f position = new();

        if (!TryResolvePosition(state, out dimension, out position))
        {
            return CommandResult.Message("§cYou must specify x y z when running this command from console.", false);
        }

        global::Basalt.Server.Entity.Entity entity;
        try
        {
            entity = new global::Basalt.Server.Entity.Entity(identifier);
        }
        catch (Exception exception)
        {
            return CommandResult.Message($"§cCould not create entity '{identifier}': {exception.Message}", false);
        }

        entity.Position = position;
        entity.Spawn(dimension!, new EntitySpawnOptions(InitialSpawn: false));

        return CommandResult.Message($"§7Summoned §a{entity.FormatIdentifier()} §7at §a{position.X:0.##} {position.Y:0.##} {position.Z:0.##}§7.", true);
    }

    static bool TryResolvePosition(CommandExecutionState state, out Dimension? dimension, out Vec3f position)
    {
        dimension = null;
        position = new Vec3f();

        PositionEnum? positionArg = state.Get<PositionEnum>("position");
        if (positionArg is not null)
        {
            dimension = state.Server.GetWorld().GetDimension(DimensionType.Overworld);
            if (dimension is null)
            {
                return false;
            }

            position = positionArg.Value;
            return true;
        }

        if (state.Executor is not PlayerExecutor executor || executor.Player.Dimension is null)
        {
            return false;
        }

        Player player = executor.Player;
        dimension = player.Dimension;
        position = player.Position;
        return true;
    }
}
