using System.Collections.Frozen;
using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Worlds.Features;
using BasaltBE.Basalt.Worlds.Structures;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class StructureCommand : Command {
    readonly FrozenDictionary<string, StructureFeature> structures;

    public StructureCommand(IEnumerable<Structure> templates) : base(
        "structure", "Loads a structure in the world.", "basalt.command.structure"
    ) {
        ArgumentNullException.ThrowIfNull(templates);
        structures = templates.ToFrozenDictionary(
            template => template.Identifier, template => new StructureFeature(template), StringComparer.Ordinal
        );
        AddOverload(
            new CommandParameter("action", new CustomEnum("StructureAction", ["load"])),
            new CommandParameter("name", new CustomEnum("Structures", structures.Keys.Order(StringComparer.Ordinal))),
            new CommandParameter("position", new PositionEnum(integer: true))
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

        string identifier = context.Get<string>("name");
        BlockPos position = context.Get<BlockPos>("position");
        FeaturePlacement placement = new(context.Server.Features, dimension, Random.Shared, int.MaxValue);
        placement.Place(structures[identifier], position.X, position.Y, position.Z);
        return !placement.Completed
            ? new(false, $"Structure '{identifier}' could not be placed at ({position.X}, {position.Y}, {position.Z}).")
            : new(true, $"Loaded a structure of name {identifier}");
    }
}
