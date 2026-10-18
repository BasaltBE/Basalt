using System.Text;
using BasaltBE.Basalt.Commands.Types;

namespace BasaltBE.Basalt.Commands;

public abstract class Command {
    public readonly string Name;
    public readonly string Description;
    public readonly string[] Aliases;
    public readonly string Permission;
    public readonly List<CommandOverload> Overloads = [];

    internal bool Concurrent;

    protected Command(
        string name,
        string description,
        string permission = "",
        string[]? aliases = null) {
        Name = name;
        Description = description;
        Permission = permission;
        Aliases = aliases is null ? [] : (string[])aliases.Clone();
    }

    protected void AddOverload(params CommandParameter[] parameters) {
        Overloads.Add(new CommandOverload((CommandParameter[])parameters.Clone()));
    }

    public virtual string GetUsage() {
        if (Overloads.Count == 0)
            return "/" + Name;

        StringBuilder usage = new();
        foreach (CommandOverload overload in Overloads) {
            if (usage.Length > 0)
                usage.Append('\n');
            usage.Append('/').Append(Name);
            foreach (CommandParameter parameter in overload.Parameters) {
                usage.Append(' ').Append(parameter.Optional ? '[' : '<')
                    .Append(parameter.Name).Append(parameter.Optional ? ']' : '>');
            }
        }
        return usage.ToString();
    }

    public abstract CommandResult Execute(CommandContext context);
}
