using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class DeopCommand : Command
{
    public DeopCommand() : base("deop", "Remove operator access from players.", "basalt.operator")
    {
        AddOverload(new CommandParameter("targets", new TargetEnum(playersOnly: true, selectorsOnly: true)));
        AddOverload(new CommandParameter("player", new StringEnum()));
    }

    public override CommandResult Execute(CommandContext context)
    {
        if (context.Arguments[0] is Entity[] targets)
        {
            if (targets.Length == 0)
                return new(false, "No players matched the target.");

            int removed = 0;
            foreach (Entity target in targets)
            {
                if (target is Player player && player.Permissions.Remove("basalt.operator"))
                    removed++;
            }
            return removed == 0
                ? new(false, "No selected players have operator access.")
                : new(true, $"Removed operator access from {removed} player(s).");
        }

        string name = context.Get<string>("player");
        PlayerPermissions? permissions = context.Server.Permissions.GetByName(name);
        if (permissions is null)
            return new(false, $"Unknown player '{name}'. The player must have joined before.");
        if (!permissions.Remove("basalt.operator"))
            return new(false, $"{name} does not have operator access.");

        return new(true, $"Removed operator access from {name}.");
    }
}
