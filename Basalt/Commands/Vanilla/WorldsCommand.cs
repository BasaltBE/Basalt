namespace Basalt.Core.Commands.Vanilla;

using Basalt.Core.Worlds;

public static class WorldsCommand
{
  public static readonly CommandDefinition Definition = new()
  {
    Name = "worlds",
    Description = "Lists all worlds or shows info about a specific world.",
    Aliases = ["world"],
    Permissions = ["basalt.op"],
    Overloads =
    [
      new OverloadDefinition { Parameters = [] },
      new OverloadDefinition
      {
        Parameters = [new ParameterDefinition { Name = "name", Type = typeof(StringEnum) }]
      }
    ],
    Handler = new CommandHandler(Execute)
  };

  static CommandResult Execute(CommandContext ctx)
  {
    string? name = ctx.Get<StringEnum>("name")?.Value;
    if (string.IsNullOrWhiteSpace(name))
      return ListWorlds(ctx);

    return ShowWorld(ctx, name);
  }

  static CommandResult ListWorlds(CommandContext ctx)
  {
    string worldsDirectory = Path.GetDirectoryName(ctx.Server.Properties.WorldPath) ?? "worlds";
    if (string.IsNullOrWhiteSpace(worldsDirectory))
      worldsDirectory = "worlds";

    HashSet<string> loadedNames = new(StringComparer.OrdinalIgnoreCase);
    foreach (var world in ctx.Server.Worlds)
      loadedNames.Add(world.Name);

    HashSet<string> allNames = new(loadedNames, StringComparer.OrdinalIgnoreCase);

    if (Directory.Exists(worldsDirectory))
    {
      foreach (string dir in Directory.GetDirectories(worldsDirectory))
      {
        string dirName = Path.GetFileName(dir);
        if (!string.IsNullOrWhiteSpace(dirName))
          allNames.Add(dirName);
      }
    }

    if (allNames.Count == 0)
      return CommandResult.OkMessage("§7No worlds found.");

    string message = $"§r§7Worlds (§a{allNames.Count}§7)\n";
    foreach (string worldName in allNames)
    {
      bool loaded = loadedNames.Contains(worldName);
      string status = loaded ? "§aLoaded" : "§cUnloaded";
      message += $"§7` {worldName} ({status}§7)\n";
    }

    return CommandResult.OkMessage(message);
  }

  static CommandResult ShowWorld(CommandContext ctx, string name)
  {
    World? world = null;
    foreach (var w in ctx.Server.Worlds)
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
        worldsDirectory = "worlds";

      string worldPath = Path.Combine(worldsDirectory, name);
      if (Directory.Exists(worldPath))
        return CommandResult.OkMessage($"§r§7World '§a{name}§7' exists but is §cunloaded§7.");

      return CommandResult.Error($"World '{name}' not found.");
    }

    int dimensionCount = world.DimensionCount;
    int entityCount = 0;
    int chunkCount = 0;

    string dimensionList = "";
    foreach (var dim in world.Dimensions)
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
}
