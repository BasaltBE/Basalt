using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Features;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class FeatureCommand : Command {
    public FeatureCommand(FeatureRegistry features) : base(
        "feature", "Places a world-generation feature.", "basalt.command.feature"
    ) {
        AddOverload(
            new CommandParameter("feature", new CustomEnum("Features", features.Identifiers, soft: true)),
            new CommandParameter("position", new PositionEnum(integer: true), optional: true),
            new CommandParameter("variant", new IntEnum(0), optional: true)
        );
    }

    public override CommandResult Execute(CommandContext context) {
        Dimension? dimension = context.Player?.Dimension;
        if (dimension is null) {
            foreach (Dimension candidate in context.Server.GetWorld().Dimensions.Values) {
                dimension = candidate;
                break;
            }
        }

        if (dimension is null) return new(false, "No dimension is available.");

        string identifier = context.Get<string>("feature");
        int x;
        int y;
        int z;
        if (context.Arguments[1] is BlockPos position) {
            x = position.X;
            y = position.Y;
            z = position.Z;
        } else if (context.Player is { } player) {
            x = (int)MathF.Floor(player.Position.X);
            y = (int)MathF.Floor(player.Position.Y);
            z = (int)MathF.Floor(player.Position.Z);
        } else {
            return new(false, "A position is required when using this command from the console.");
        }

        int placed = context.Server.Features.Place(identifier, dimension, x, y, z, context.Get<int?>("variant"));
        if (placed == 0) return new(false, $"Feature '{identifier}' could not be placed at ({x}, {y}, {z}).");
        return new(true, $"Placed feature '{identifier}' at ({x}, {y}, {z}) with {placed} block(s).");
    }
}
