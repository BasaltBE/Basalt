using System.Text;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds;
using BasaltBE.Basalt.Worlds.Dimensions;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class ListCommand : Command {
    public ListCommand() : base("list", "List connected players.") {
        Concurrent = true;
        AddOverload();
    }

    public override CommandResult Execute(CommandContext context) {
        StringBuilder names = new();
        int count = 0;
        foreach (World world in context.Server.Worlds) {
            foreach (Dimension dimension in world.Dimensions.Values) {
                foreach (Player player in dimension.GetPlayers()) {
                    if (!player.Spawned || player.Disposed)
                        continue;
                    if (count++ > 0) names.Append(", ");
                    names.Append(player.Username);
                }
            }
        }
        return new(true, FormattableString.Invariant($"Players ({count}/{context.Server.Properties.MaxPlayers}): {names}"));
    }
}
