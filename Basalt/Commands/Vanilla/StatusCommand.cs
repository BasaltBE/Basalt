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
        var dimensions = ctx.Server.Worlds.SelectMany(w => w.Dimensions).ToArray();
        var network = ctx.Server.Network;

        int worldCount = ctx.Server.Worlds.Count();
        int playerCount = ctx.Server.GetPlayersSnapshot().Length;
        int entityCount = dimensions.Sum(d => d.GetEntitiesSnapshot().Length);
        int chunkCount = dimensions.Sum(d => d.ChunkCount);
        int pendingChunkRequests = dimensions.Sum(d => d.PendingChunkRequestCount);
        int pendingChunkCallbacks = dimensions.Sum(d => d.PendingChunkCallbackCount);
        double slowestDimension = dimensions.Length == 0 ? 0 : dimensions.Max(d => d.TickWork);
        int activeDomains = dimensions.Count(d => d.ActiveEntityCount > 0);

        using var process = System.Diagnostics.Process.GetCurrentProcess();
        double heapMb = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
        double workingSetMb = process.WorkingSet64 / 1024.0 / 1024.0;
        double privateMb = process.PrivateMemorySize64 / 1024.0 / 1024.0;
        double sentMb = network.SentBytes / 1024.0 / 1024.0;

        string message = $"§r§7Server Status ({color}{tps:0.0}§7)\n" +
                         $"§7` TPS ({color}{tps:0.0}§7)\n" +
                         $"§7` Tick Work (§a{ctx.Server.TickWork:0.00} ms last, {ctx.Server.TickWorkAverage:0.00} ms avg, {ctx.Server.TickWorkMaximum:0.00} ms max§7)\n" +
                         $"§7` Players (§a{playerCount}§7)\n" +
                         $"§7` Worlds (§a{worldCount}§7)\n" +
                         $"§7` Dimensions (§a{dimensions.Length}§7)\n" +
                         $"§7` Entities (§a{entityCount}§7)\n" +
                         $"§7` Loaded Chunks (§a{chunkCount}§7)\n" +
                         $"§7` Pending Chunks (§a{pendingChunkRequests} requests, {pendingChunkCallbacks} callbacks§7)\n" +
                         $"§7` Active Domains (§a{activeDomains}, {slowestDimension:0.00} ms slowest§7)\n" +
                         $"§7` Worker Queue (§a{ctx.Server.WorkerPool.PendingWorkCount} work, {ctx.Server.WorkerPool.PendingCompletionCount} completions, {ctx.Server.WorkerPool.AverageQueueWaitMilliseconds:0.00} ms wait§7)\n" +
                         $"§7` Network Queue (§a{network.PendingIncomingFrameCount} frames, {network.PendingIncomingPacketCount} packets in, {network.PendingOutgoingPacketCount} packets out§7)\n" +
                         $"§7` Network Sent (§a{sentMb:0.0} MB, {network.SentPackets} packets, {network.SentFrames} frames§7)\n" +
                         $"§7` Heap (§a{heapMb:0.0} MB§7)\n" +
                         $"§7` Working Set (§a{workingSetMb:0.0} MB§7)\n" +
                         $"§7` Private Memory (§a{privateMb:0.0} MB§7)\n" +
                         $"§7` GC Collections (§aG0 {GC.CollectionCount(0)}, G1 {GC.CollectionCount(1)}, G2 {GC.CollectionCount(2)}§7)\n";

        return CommandResult.OkMessage(message);
    }
}
