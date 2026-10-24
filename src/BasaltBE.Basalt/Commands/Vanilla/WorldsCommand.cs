using System.Text;
using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Worlds.Dimensions;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class WorldsCommand : Command {
    private static readonly CustomEnum CreateActionEnum = new("worlds_create_action", ["create"]);
    private static readonly CustomEnum DimensionActionEnum = new("worlds_dimension_action", ["dimension"]);
    private static readonly CustomEnum TeleportActionEnum = new("worlds_teleport_action", ["tp"]);
    private static readonly CustomEnum DimensionOperationEnum = new("worlds_dimension_operation", ["add"]);
    private static readonly CustomEnum DimensionTypeEnum = new("worlds_dimension_type", ["overworld", "nether", "end"]);

    public WorldsCommand() : base("worlds", "List worlds or move to a world.", "basalt.command.worlds") {
        AddOverload();
        AddOverload(
            new("action", CreateActionEnum),
            new("name", new StringEnum())
        );
        AddOverload(
            new("action", DimensionActionEnum),
            new("operation", DimensionOperationEnum),
            new("world", new StringEnum()),
            new("id", new StringEnum()),
            new("type", DimensionTypeEnum),
            new("generator", new StringEnum())
        );
        AddOverload(
            new("action", TeleportActionEnum),
            new("world", new StringEnum()),
            new("dimension", new StringEnum(), optional: true)
        );
    }

    public override string GetUsage() => string.Join('\n',
        "/worlds",
        "/worlds create <name>",
        "/worlds dimension add <world> <id> <type> <generator>",
        "/worlds tp <world> [dimension]"
    );

    public override CommandResult Execute(CommandContext context) {
        if (context.Parameters.Length == 0)
            return ListWorlds(context.Server.Worlds);

        if (context.Parameters[0].Name == "action") {
            if (context.Get<string>("action") == "create" && context.Parameters.Length == 2) {
                string name = context.Get<string>("name");
                try {
                    context.Server.CreateWorld(
                        name,
                        static world => world.CreateDimension("minecraft:overworld", DimensionType.Overworld),
                        providerName: context.Server.Properties.WorldProvider
                    );
                }
                catch (Exception error) when (
                    error is ArgumentException or InvalidOperationException or KeyNotFoundException or IOException or UnauthorizedAccessException
                ) {
                    return new(false, error.Message);
                }
                return new(true, $"Created world {name}.");
            }

            if (context.Get<string>("action") == "dimension" && context.Parameters.Length == 6) {
                World targetWorld;
                try {
                    targetWorld = context.Server.GetWorld(context.Get<string>("world"));
                }
                catch (KeyNotFoundException) {
                    return new(false, "That world is not loaded.");
                }

                DimensionType type = Enum.Parse<DimensionType>(context.Get<string>("type"), ignoreCase: true);
                string dimensionIdentifier = context.Get<string>("id");
                try {
                    targetWorld.CreateDimension(dimensionIdentifier, type, context.Get<string>("generator"));
                }
                catch (Exception error) when (
                    error is ArgumentException or InvalidOperationException or KeyNotFoundException
                ) {
                    return new(false, error.Message);
                }
                return new(true, $"Added dimension {dimensionIdentifier} ({context.Get<string>("type")}) to {targetWorld.Identifier}.");
            }

            if (context.Get<string>("action") == "tp" && context.Parameters.Length == 3) {
                if (context.Player is not { Spawned: true, Disposed: false } player)
                    return new(false, "Only a spawned player can switch worlds.");

                World world;
                try {
                    world = context.Server.GetWorld(context.Get<string>("world"));
                }
                catch (KeyNotFoundException) {
                    return new(false, "That world is not loaded.");
                }

                Dimension? dimension = context.Arguments[2] is string identifier
                    ? world.GetDimension(identifier)
                    : world.GetDimension(DimensionType.Overworld);
                if (dimension is null && context.Arguments[2] is null)
                    foreach (Dimension candidate in world.Dimensions.Values) {
                        dimension = candidate;
                        break;
                    }
                if (dimension is null)
                    return new(false, "That dimension is not configured in the world.");

                if (ReferenceEquals(player.Dimension, dimension))
                    return new(true, $"You are already in {world.Identifier}/{dimension.Identifier}.");

                player.TransferTo(dimension);
                return new(true, $"Moved to {world.Identifier}/{dimension.Identifier}.");
            }

            return new(false, GetUsage());
        }

        return new(false, GetUsage());
    }

    private static CommandResult ListWorlds(IReadOnlyList<World> worlds) {
        StringBuilder output = new();
        output.Append("§7Worlds (§w").Append(worlds.Count).Append("§7)§r");
        foreach (World world in worlds) {
            int chunks = 0;
            foreach (Dimension dimension in world.Dimensions.Values)
                chunks += dimension.ChunkCount;

            output.Append("\n§8` §a").Append(world.Identifier)
                .Append(" (§w").Append(world.Dimensions.Count).Append("§7 dimensions, §w")
                .Append(chunks).Append("§7 chunks)§r");
            foreach (Dimension dimension in world.Dimensions.Values) {
                output.Append("\n§8  ` §a").Append(dimension.Identifier)
                    .Append(" (§w").Append(dimension.CustomDefinition?.Identifier ?? dimension.Type.ToString().ToLowerInvariant())
                    .Append("§7, §w").Append(dimension.ChunkCount).Append("§7 chunks)§r");
            }
        }
        return new(true, output.ToString());
    }
}
