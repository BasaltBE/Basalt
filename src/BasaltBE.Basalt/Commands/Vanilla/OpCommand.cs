using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Permissions;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class OpCommand : Command
{
    public OpCommand() : base("op", "Grant operator access to players.", "basalt.operator")
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

            int granted = 0;
            foreach (Entity target in targets)
            {
                if (target is Player player &&
                    context.Server.Permissions.Get(player.Xuid, player.Username).Add("basalt.operator"))
                    granted++;
            }
            return granted == 0
                ? new(false, "All selected players already have operator access.")
                : new(true, $"Granted operator access to {granted} player(s).");
        }

        string name = context.Get<string>("player");
        PlayerPermissions? permissions = context.Server.Permissions.GetByName(name);
        if (permissions is null)
            return new(false, $"Unknown player '{name}'. The player must have joined before.");
        if (!permissions.Add("basalt.operator"))
            return new(false, $"{name} already has operator access.");

        return new(true, $"Granted operator access to {name}.");
    }
}
