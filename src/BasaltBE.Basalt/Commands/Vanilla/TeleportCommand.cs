using System.Globalization;
using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class TeleportCommand : Command {
    public TeleportCommand() : base("tp", "Teleports entities (players, mobs, etc.).", "basalt.command.tp", ["teleport"]) {
        AddOverload(new CommandParameter("destination", new TargetEnum(playersOnly: true)));
        AddOverload(new("victim", new TargetEnum(playersOnly: true)), new("destination", new TargetEnum(playersOnly: true)));
        AddOverload(new CommandParameter("position", new PositionEnum()));
        AddOverload(new("victim", new TargetEnum(playersOnly: true)), new("position", new PositionEnum()));
    }

    public override CommandResult Execute(CommandContext context) {
        Entity[]? selected = null;
        foreach (CommandParameter parameter in context.Parameters)
            if (parameter.Name == "victim") selected = context.Get<Entity[]?>("victim");
        if (selected is null && context.Player is { } sender) selected = [sender];
        if (selected is null || selected.Length == 0) return new(false, "No players matched the target.");
        if (selected.Length > 128) return new(false, "Select no more than 128 players.");

        Dimension? dimension = null;
        Vec3? position = null;
        string? destinationName = null;
        foreach (CommandParameter parameter in context.Parameters) {
            if (parameter.Name == "destination") {
                Entity[] destinations = context.Get<Entity[]>("destination");
                if (destinations.Length != 1 || destinations[0] is not Player destination)
                    return new(false, "Select one destination player.");

                dimension = destination.Dimension;
                position = new Vec3 { X = destination.Position.X, Y = destination.Position.Y, Z = destination.Position.Z };
                destinationName = destination.Username;
            }
            if (parameter.Name == "position") position = context.Get<Vec3>("position");
        }

        if (position is null || MathF.Abs(position.X) > 30_000_000 || MathF.Abs(position.Y) > 30_000_000 ||
            MathF.Abs(position.Z) > 30_000_000)
            return new(false, "Cannot teleport entities outside of the world");

        List<string> moved = [];
        foreach (Entity entity in selected) {
            if (entity is not Player { Spawned: true, Disposed: false } player) continue;

            player.TransferTo(dimension ?? player.Dimension, position);
            moved.Add(player.Username);
        }
        if (moved.Count == 0) return new(false, "No selected players are online and spawned.");

        string destinationText = destinationName ?? string.Create(CultureInfo.InvariantCulture,
            $"{position.X:0.###}, {position.Y:0.###}, {position.Z:0.###}");
        return new(true, $"Teleported {string.Join(", ", moved)} to {destinationText}");
    }
}
