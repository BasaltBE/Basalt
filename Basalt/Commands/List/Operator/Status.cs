using Basalt.Commands;

public class StatusCommand : Command
{
    public StatusCommand() : base("status", "Get the status of the server")
    {
        Permissions.Add("basalt.op");
    }

    public override CommandResult Execute(CommandExecutionState state)
    {
        var tps = state.Server.Tps;
        var color = tps < 10 ? "§c" : tps < 15 ? "§6" : "§a";

        var worldCount = state.Server.Worlds.Count();
        var dimensionCount = state.Server.Worlds
            .SelectMany(w => w.Dimensions)
            .Count();
        var entityCount = state.Server.Worlds
            .SelectMany(w => w.Dimensions)
            .Sum(d => d.Entities.Count);


        using var process = System.Diagnostics.Process.GetCurrentProcess();
        var heapMb = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
        var workingSetMb = process.WorkingSet64 / 1024.0 / 1024.0;
        var privateMb = process.PrivateMemorySize64 / 1024.0 / 1024.0;

        var message = $"§r§7Server Status ({color}{tps:0.0}§7)\n" +
                      $"§7` TPS ({color}{tps:0.0}§7)\n" +
                      $"§7` Worlds (§a{worldCount}§7)\n" +
                      $"§7` Dimensions (§a{dimensionCount}§7)\n" +
                      $"§7` Entities (§a{entityCount}§7)\n" +
                      $"§7` Heap (§a{heapMb:0.0} MB§7)\n" +
                      $"§7` Working Set (§a{workingSetMb:0.0} MB§7)\n" +
                      $"§7` Private Memory (§a{privateMb:0.0} MB§7)\n";

        return CommandResult.Message(message, true);
    }
}
