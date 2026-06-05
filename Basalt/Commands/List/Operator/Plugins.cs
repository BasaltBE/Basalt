namespace Basalt.Server.Commands.List.Operator;

using Basalt.Server.Commands;
using Basalt.Server.Plugins;

public class PluginsCommand : Command
{
    public PluginsCommand() : base("plugins", "List loaded plugins")
    {
        Permissions.Add("basalt.op");
    }

    public override CommandResult Execute(CommandExecutionState state)
    {
        PluginContainer[] plugins = state.Server.Plugins.Plugins
            .OrderBy(plugin => plugin.Description.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (plugins.Length == 0)
        {
            return CommandResult.Message("§r§7Plugins (§a0§7)\n§7` No plugins loaded.", true);
        }

        string message = $"§r§7Plugins (§a{plugins.Length}§7)\n";
        for (int i = 0; i < plugins.Length; i++)
        {
            PluginDescription description = plugins[i].Description;
            string authors = description.Authors.Length == 0
                ? "Unknown"
                : string.Join(", ", description.Authors);

            message += $"§7` §a{description.Name} §7v§a{description.Version} §7by §a{authors}§7\n";
        }

        return CommandResult.Message(message, true);
    }
}
