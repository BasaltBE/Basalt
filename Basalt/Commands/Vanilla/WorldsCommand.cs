namespace Basalt.Core.Commands.Vanilla;

using Basalt.Core.Worlds;
using Basalt.Protocol.Types;
using Dimension = Basalt.Core.Worlds.Dimensions.Dimension;
using Player = Player.Player;

public static class WorldsCommand
{
    public static readonly CommandDefinition Definition = new()
    {
        Name = "worlds",
        Description = "Lists, inspects, or teleports to worlds.",
        Aliases = ["world"],
        Permissions = ["basalt.op"],
        Overloads =
        [
            // /worlds
            new OverloadDefinition { Parameters = [] },
            // /worlds <name>
            new OverloadDefinition
            {
                Parameters = [new ParameterDefinition { Name = "name", Type = typeof(StringEnum) }]
            },
            // /worlds tp <name>
            new OverloadDefinition
            {
                Parameters =
                [
                    new ParameterDefinition { Name = "action", Type = typeof(StringEnum) },
                    new ParameterDefinition { Name = "name", Type = typeof(StringEnum) }
                ]
            }
        ],
        Handler = new CommandHandler(Execute)
    };

    private static CommandResult Execute(CommandContext ctx)
    {
        string? action = ctx.Get<StringEnum>("action")?.Value;
        string? name = ctx.Get<StringEnum>("name")?.Value;

        if (string.Equals(action, "tp", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return CommandResult.Error("Usage: /worlds tp <name>");
            }
            return TeleportToWorld(ctx, name);
        }

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(action))
        {
            return ListWorlds(ctx);
        }

        string worldName = name ?? action!;
        return ShowWorld(ctx, worldName);
    }

    private static CommandResult ListWorlds(CommandContext ctx)
    {
        string worldsDirectory = Path.GetDirectoryName(ctx.Server.Properties.WorldPath) ?? "worlds";
        if (string.IsNullOrWhiteSpace(worldsDirectory))
        {
            worldsDirectory = "worlds";
        }

        HashSet<string> loadedNames = new(StringComparer.OrdinalIgnoreCase);
        foreach (World world in ctx.Server.Worlds)
        {
            loadedNames.Add(world.Name);
        }

        HashSet<string> allNames = new(loadedNames, StringComparer.OrdinalIgnoreCase);

        if (Directory.Exists(worldsDirectory))
        {
            foreach (string dir in Directory.GetDirectories(worldsDirectory))
            {
                string dirName = Path.GetFileName(dir);
                if (!string.IsNullOrWhiteSpace(dirName))
                {
                    allNames.Add(dirName);
                }
            }
        }

        if (allNames.Count == 0)
        {
            return CommandResult.OkMessage("§7No worlds found.");
        }

        string message = $"§r§7Worlds (§a{allNames.Count}§7)\n";
        foreach (string worldName in allNames)
        {
            bool loaded = loadedNames.Contains(worldName);
            string status = loaded ? "§aLoaded" : "§cUnloaded";
            message += $"§7` {worldName} ({status}§7)\n";
        }

        return CommandResult.OkMessage(message);
    }

    private static CommandResult ShowWorld(CommandContext ctx, string name)
    {
        World? world = null;
        foreach (World w in ctx.Server.Worlds)
        {
            if (string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                world = w;
                break;
            }
        }

        if (world is null)
        {
            string worldsDirectory = Path.GetDirectoryName(ctx.Server.Properties.WorldPath) ?? "worlds";
            if (string.IsNullOrWhiteSpace(worldsDirectory))
            {
                worldsDirectory = "worlds";
            }

            string worldPath = Path.Combine(worldsDirectory, name);
            if (Directory.Exists(worldPath))
            {
                return CommandResult.OkMessage($"§r§7World '§a{name}§7' exists but is §cunloaded§7.");
            }

            return CommandResult.Error($"World '{name}' not found.");
        }

        int dimensionCount = world.DimensionCount;
        int entityCount = 0;
        int chunkCount = 0;

        string dimensionList = "";
        foreach (Dimension dim in world.Dimensions)
        {
            entityCount += dim.Entities.Count;
            chunkCount += dim.ChunkCount;
            dimensionList += $"§7  ` {dim.Identifier} (§a{dim.Entities.Count}§7 entities, §a{dim.ChunkCount}§7 chunks)\n";
        }

        string message = $"§r§7World '§a{world.Name}§7' (§aLoaded§7)\n" +
                         $"§7` Tick (§a{world.TickValue}§7)\n" +
                         $"§7` Dimensions (§a{dimensionCount}§7)\n" +
                         dimensionList +
                         $"§7` Total Entities (§a{entityCount}§7)\n" +
                         $"§7` Total Chunks (§a{chunkCount}§7)\n";

        return CommandResult.OkMessage(message);
    }

    private static CommandResult TeleportToWorld(CommandContext ctx, string name)
    {
        Player? player = ctx.RequirePlayer(out CommandResult? error);
        if (player is null)
        {
            return error!;
        }

        World? world = null;
        foreach (World w in ctx.Server.Worlds)
        {
            if (string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                world = w;
                break;
            }
        }

        if (world is null)
        {
            return CommandResult.Error($"World '{name}' is not loaded.");
        }

        Dimension? targetDimension = world.Dimensions.FirstOrDefault();
        if (targetDimension is null)
        {
            return CommandResult.Error($"World '{name}' has no dimensions.");
        }

        Vec3f spawnPosition = new() { X = 0f, Y = -57f, Z = 0f };
        player.Teleport(spawnPosition, targetDimension);

        return CommandResult.OkMessage($"§7Teleported to world '§a{world.Name}§7'.");
    }
}
