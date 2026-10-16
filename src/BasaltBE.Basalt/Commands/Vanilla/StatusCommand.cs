using System.Globalization;
using System.Text;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Worlds.Dimensions;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class StatusCommand : Command
{
    public StatusCommand() : base("status", "Show server runtime statistics.")
    {
        AddOverload();
    }

    public override CommandResult Execute(CommandContext context)
    {
        int players = 0;
        int entities = 0;
        foreach (World world in context.Server.Worlds)
        {
            foreach (Dimension dimension in world.Dimensions.Values)
            {
                entities += dimension.EntityCount;
                foreach (Player player in dimension.GetPlayers())
                {
                    if (player.Spawned && !player.Disposed)
                        players++;
                }
            }
        }

        var ticks = context.Server.Ticks;
        string tpsColor = ticks.Tps >= 19 ? "§a" : "§c";
        string msptColor = ticks.AverageMilliseconds < 40 ? "§a" : "§c";
        string lastColor = ticks.LastMilliseconds >= 40 ? "§c" : ticks.LastMilliseconds >= 20 ? "§e" : "§a";
        string peakColor = ticks.MaximumMilliseconds >= 40 ? "§c" : ticks.MaximumMilliseconds >= 20 ? "§e" : "§a";
        StringBuilder response = new();
        response.Append("§7Server Status§r\n");
        response.Append(CultureInfo.InvariantCulture,
            $"§8` §7TPS ({tpsColor}{ticks.Tps:F2}§7)  MSPT ({msptColor}{ticks.AverageMilliseconds:F3}§7)§r\n");
        response.Append(CultureInfo.InvariantCulture,
            $"§8` §7Tick ({lastColor}{ticks.LastMilliseconds:F3}§7 ms last, {peakColor}{ticks.MaximumMilliseconds:F3}§7 ms peak)§r\n");
        response.Append(CultureInfo.InvariantCulture,
            $"§8` §7Memory: §w{GC.GetTotalMemory(false) / 1048576.0:F1}§7 MiB heap§r\n");
        response.Append(CultureInfo.InvariantCulture,
            $"§8` §7Players (§w{players}/{context.Server.Properties.MaxPlayers}§7)  Entities: (§w{entities}§7)§r\n");
        response.Append(CultureInfo.InvariantCulture,
            $"§8` §7Worlds (§w{context.Server.Worlds.Count}§7)§r");

        foreach (World world in context.Server.Worlds)
        {
            int chunks = 0;
            int pending = 0;
            foreach (Dimension dimension in world.Dimensions.Values)
            {
                chunks += dimension.ChunkCount;
                pending += dimension.PendingRequestCount;
            }
            string worldMsptColor = world.Ticks.AverageMilliseconds < 40 ? "§a" : "§c";
            response.Append(CultureInfo.InvariantCulture,
                $"\n§8  ` §7{world.Identifier} (§w{chunks}§7 chunks, §w{pending}§7 pending, {worldMsptColor}{world.Ticks.AverageMilliseconds:F3}§7 ms)§r");
        }
        return new(true, response.ToString());
    }
}
