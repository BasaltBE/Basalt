namespace Basalt.Core.Commands.Vanilla;

public static class StatusCommand {
    public static readonly CommandDefinition Definition = new() {
        Name = "status",
        Description = "Get the status of the server.",
        Permissions = ["basalt.op"],
        Overloads = [new OverloadDefinition { Parameters = [] }],
        Handler = new CommandHandler(Execute)
    };

    static CommandResult Execute(CommandContext ctx) {
        double tps = ctx.Server.Tps;
        string color = tps < 10 ? "§c" : tps < 15 ? "§6" : "§a";

        int worldCount = ctx.Server.Worlds.Count();
        int dimensionCount = ctx.Server.Worlds.SelectMany(w => w.Dimensions).Count();
        int entityCount = ctx.Server.Worlds.SelectMany(w => w.Dimensions).Sum(d => d.Entities.Count);

        using var process = System.Diagnostics.Process.GetCurrentProcess();
        double heapMb = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
        double workingSetMb = process.WorkingSet64 / 1024.0 / 1024.0;
        double privateMb = process.PrivateMemorySize64 / 1024.0 / 1024.0;

        string message = $"§r§7Server Status ({color}{tps:0.0}§7)\n" +
                         $"§7` TPS ({color}{tps:0.0}§7)\n" +
                         $"§7` Worlds (§a{worldCount}§7)\n" +
                         $"§7` Dimensions (§a{dimensionCount}§7)\n" +
                         $"§7` Entities (§a{entityCount}§7)\n" +
                         $"§7` Heap (§a{heapMb:0.0} MB§7)\n" +
                         $"§7` Working Set (§a{workingSetMb:0.0} MB§7)\n" +
                         $"§7` Private Memory (§a{privateMb:0.0} MB§7)\n";

        return CommandResult.OkMessage(message);
    }
}
