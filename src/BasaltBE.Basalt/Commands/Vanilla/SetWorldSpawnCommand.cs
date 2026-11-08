using System.Globalization;
using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class SetWorldSpawnCommand : Command {
    public SetWorldSpawnCommand() : base("setworldspawn", "Sets the world spawn.", "basalt.command.setworldspawn") {
        AddOverload(new CommandParameter("position", new PositionEnum(integer: true), optional: true));
    }

    public override CommandResult Execute(CommandContext context) {
        World world = context.Player?.World ?? context.Server.GetWorld();
        Dimension? dimension = world.GetDimension(DimensionType.Overworld);
        if (dimension is null || context.Player is { } player && !ReferenceEquals(player.Dimension, dimension))
            return new(false, "The world spawn can not be set in this dimension");

        BlockPos? position = context.Get<BlockPos?>("position");
        if (position is null) {
            if (context.Player is not { } sender) return new(false, "Specify a spawn position.");
            position = new BlockPos {
                X = (int)MathF.Floor(sender.Position.X),
                Y = (int)MathF.Floor(sender.Position.Y),
                Z = (int)MathF.Floor(sender.Position.Z)
            };
        }
        if (Math.Abs((long)position.X) > 30_000_000 || Math.Abs((long)position.Z) > 30_000_000 ||
            position.Y < dimension.MinimumY || position.Y > dimension.MaximumY)
            return new(false, "Cannot set the world spawn outside of the world.");

        world.SetSpawn(position);
        return new(true, string.Create(CultureInfo.InvariantCulture,
            $"Set the world spawn point to ({position.X}, {position.Y}, {position.Z})"));
    }
}
