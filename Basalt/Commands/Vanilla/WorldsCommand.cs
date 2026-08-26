namespace Basalt.Core.Commands.Vanilla;

using System.Text;
using Basalt.BedrockProtocol.Types;
using Basalt.Core.Worlds;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Core.Worlds.Dimensions.Generation;
using Dimension = Basalt.Core.Worlds.Dimensions.Dimension;
using Player = Player.Player;

public static class WorldsCommand {
    public static readonly CommandDefinition Definition = new() {
        Name = "worlds",
        Description = "Lists, inspects, or teleports to worlds.",
        Aliases = [],
        Permissions = ["basalt.op"],
        Overloads =
        [
            new OverloadDefinition { Parameters = [] },
            new OverloadDefinition {
                Parameters = [new ParameterDefinition { Name = "name", Type = typeof(StringEnum) }]
            },
            new OverloadDefinition {
                Parameters =
                [
                    new ParameterDefinition { Name = "action", Type = typeof(StringEnum) },
                    new ParameterDefinition { Name = "name", Type = typeof(StringEnum) }
                ]
            }
        ],
        Handler = new CommandHandler(Execute)
    };

    private static CommandResult Execute(CommandContext ctx) {
        string? action = ctx.Get<StringEnum>("action")?.Value;
        string? name = ctx.Get<StringEnum>("name")?.Value;

        if (string.Equals(action, "tp", StringComparison.OrdinalIgnoreCase)) {
            if (string.IsNullOrWhiteSpace(name)) {
                return CommandResult.Error("Usage: /worlds tp <name>");
            }

            return TeleportToWorld(ctx, name);
        }

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(action)) {
            return ListWorlds(ctx);
        }

        return ShowWorld(ctx, name ?? action!);
    }

    private static CommandResult ListWorlds(CommandContext ctx) {
        string worldsDirectory = ctx.Server.Properties.WorldPath;
        if (string.IsNullOrWhiteSpace(worldsDirectory)) {
            worldsDirectory = "worlds";
        }

        Dictionary<string, World> worldsByName = new(StringComparer.OrdinalIgnoreCase);
        foreach (World world in ctx.Server.Worlds) {
            worldsByName[world.Name] = world;
        }

        HashSet<string> allNames = new(worldsByName.Keys, StringComparer.OrdinalIgnoreCase);
        if (Directory.Exists(worldsDirectory)) {
            foreach (string directory in Directory.GetDirectories(worldsDirectory)) {
                string directoryName = Path.GetFileName(directory);
                if (!string.IsNullOrWhiteSpace(directoryName)) {
                    allNames.Add(directoryName);
                }
            }
        }

        if (allNames.Count == 0) {
            return CommandResult.OkMessage("\u00a77No worlds found.");
        }

        StringBuilder message = new($"\u00a7r\u00a77Worlds (\u00a7a{allNames.Count}\u00a77)\n");
        foreach (string worldName in allNames) {
            if (!worldsByName.TryGetValue(worldName, out World? world)) {
                message.Append($"\u00a77` {worldName} (\u00a7cUnloaded\u00a77)\n");
                continue;
            }

            int entityCount = world.Dimensions.Sum(dimension => dimension.GetEntitiesSnapshot().Length);
            message.Append($"\u00a77` {world.Name} (\u00a7aLoaded\u00a77, \u00a7a{world.TickWork:0.00} ms\u00a77, \u00a7a{entityCount} entities\u00a77)\n");
            foreach (Dimension dimension in world.Dimensions) {
                message.Append($"\u00a77  ` {dimension.Identifier} (\u00a7a{dimension.TickWork:0.00} ms\u00a77, \u00a7a{dimension.GetEntitiesSnapshot().Length} entities\u00a77, \u00a7a{dimension.ChunkCount} chunks\u00a77)\n");
            }
        }

        return CommandResult.OkMessage(message.ToString());
    }

    private static CommandResult ShowWorld(CommandContext ctx, string name) {
        World? world = ctx.Server.Worlds.FirstOrDefault(world =>
            string.Equals(world.Name, name, StringComparison.OrdinalIgnoreCase));

        if (world is null) {
            string worldsDirectory = ctx.Server.Properties.WorldPath;
            if (string.IsNullOrWhiteSpace(worldsDirectory)) {
                worldsDirectory = "worlds";
            }

            string worldPath = Path.Combine(worldsDirectory, name);
            if (Directory.Exists(worldPath)) {
                return CommandResult.OkMessage($"\u00a7r\u00a77World '\u00a7a{name}\u00a77' exists but is \u00a7cunloaded\u00a77.");
            }

            return CommandResult.Error($"World '{name}' not found.");
        }

        int entityCount = 0;
        int chunkCount = 0;
        StringBuilder dimensionList = new();

        foreach (Dimension dimension in world.Dimensions) {
            int dimensionEntityCount = dimension.GetEntitiesSnapshot().Length;
            entityCount += dimensionEntityCount;
            chunkCount += dimension.ChunkCount;
            dimensionList.Append($"\u00a77  ` {dimension.Identifier} (\u00a7a{dimensionEntityCount}\u00a77 entities, \u00a7a{dimension.ChunkCount}\u00a77 chunks)\n");
        }

        StringBuilder message = new();
        message.Append($"\u00a7r\u00a77World '\u00a7a{world.Name}\u00a77' (\u00a7aLoaded\u00a77)\n");
        message.Append($"\u00a77` Tick (\u00a7a{world.TickValue}\u00a77)\n");
        message.Append($"\u00a77` Dimensions (\u00a7a{world.DimensionCount}\u00a77)\n");
        message.Append(dimensionList);
        message.Append($"\u00a77` Total Entities (\u00a7a{entityCount}\u00a77)\n");
        message.Append($"\u00a77` Total Chunks (\u00a7a{chunkCount}\u00a77)\n");

        return CommandResult.OkMessage(message.ToString());
    }

    private static CommandResult TeleportToWorld(CommandContext ctx, string name) {
        Player? player = ctx.RequirePlayer(out CommandResult? error);
        if (player is null) {
            return error!;
        }

        World? world = ctx.Server.Worlds.FirstOrDefault(world =>
            string.Equals(world.Name, name, StringComparison.OrdinalIgnoreCase));

        if (world is null) {
            string worldsDirectory = ctx.Server.Properties.WorldPath;
            if (string.IsNullOrWhiteSpace(worldsDirectory)) {
                worldsDirectory = "worlds";
            }

            string worldPath = Path.Combine(worldsDirectory, name);
            if (!Directory.Exists(worldPath)) {
                return CommandResult.Error($"World '{name}' not found.");
            }

            world = ctx.Server.LoadWorld(name, "leveldb", worldPath)
                ?? ctx.Server.CreateWorld(name, "leveldb", worldPath);
        }

        if (world.DimensionCount == 0) {
            world.CreateDimension("overworld", DimensionId.Overworld, typeof(VoidGenerator));
        }

        Dimension? targetDimension = world.Dimensions.FirstOrDefault();
        if (targetDimension is null) {
            return CommandResult.Error($"World '{name}' has no dimensions.");
        }

        player.Teleport(targetDimension.SpawnPosition, targetDimension);
        return CommandResult.OkMessage($"\u00a77Teleported to world '\u00a7a{world.Name}\u00a77'.");
    }
}
