using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class DeopCommand : Command {
    public DeopCommand() : base("deop", "Revokes operator status from a player.", "basalt.operator") {
        AddOverload(new CommandParameter("targets", new TargetEnum(playersOnly: true, selectorsOnly: true)));
        AddOverload(new CommandParameter("player", new StringEnum()));
    }

    public override CommandResult Execute(CommandContext context) {
        if (context.Arguments[0] is Entity[] targets) {
            if (targets.Length == 0)
                return new(false, "No players matched the target.");

            List<string> removed = [];
            foreach (Entity target in targets) {
                if (target is Player player && player.Permissions.Remove("basalt.operator"))
                    removed.Add(player.Username);
            }
            return removed.Count == 0
                ? new(false, $"Could not de-op (permission level too high): {string.Join(", ", targets.OfType<Player>().Select(static player => player.Username))}.")
                : new(true, $"De-opped: {string.Join(", ", removed)}.");
        }

        string name = context.Get<string>("player");
        PlayerPermissions? permissions = context.Server.Permissions.GetByName(name);
        if (permissions is null)
            return new(false, $"Unknown player '{name}'. The player must have joined before.");
        if (!permissions.Remove("basalt.operator"))
            return new(false, $"Could not de-op (permission level too high): {name}.");

        return new(true, $"De-opped: {name}.");
    }
}
