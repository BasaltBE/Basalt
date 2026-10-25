using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Permissions;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class OpCommand : Command {
    public OpCommand() : base("op", "Grants operator status to a player.", "basalt.operator") {
        AddOverload(new CommandParameter("targets", new TargetEnum(playersOnly: true, selectorsOnly: true)));
        AddOverload(new CommandParameter("player", new StringEnum()));
    }

    public override CommandResult Execute(CommandContext context) {
        if (context.Arguments[0] is Entity[] targets) {
            if (targets.Length == 0)
                return new(false, "No players matched the target.");

            List<string> granted = [];
            foreach (Entity target in targets) {
                if (target is Player player &&
                    context.Server.Permissions.Get(player.Xuid, player.Username).Add("basalt.operator"))
                    granted.Add(player.Username);
            }
            return granted.Count == 0
                ? new(false, $"Could not op (already op or higher): {string.Join(", ", targets.OfType<Player>().Select(static player => player.Username))}.")
                : new(true, $"Opped: {string.Join(", ", granted)}.");
        }

        string name = context.Get<string>("player");
        PlayerPermissions? permissions = context.Server.Permissions.GetByName(name);
        if (permissions is null)
            return new(false, $"Unknown player '{name}'. The player must have joined before.");
        if (!permissions.Add("basalt.operator"))
            return new(false, $"Could not op (already op or higher): {name}.");

        return new(true, $"Opped: {name}.");
    }
}
