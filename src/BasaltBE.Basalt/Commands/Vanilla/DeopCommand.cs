using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class DeopCommand : Command
{
    public DeopCommand() : base("deop", "Remove operator access from players.", "basalt.operator")
    {
        AddOverload(new CommandParameter("player", new StringEnum()));
    }

    public override CommandResult Execute(CommandContext context)
    {
        string name = context.Get<string>("player");
        PlayerPermissions? permissions = context.Server.Permissions.GetByName(name);
        if (permissions is null)
            return new(false, $"Unknown player '{name}'. The player must have joined before.");
        if (!permissions.Remove("basalt.operator"))
            return new(false, $"{name} does not have operator access.");

        return new(true, $"Removed operator access from {name}.");
    }
}
