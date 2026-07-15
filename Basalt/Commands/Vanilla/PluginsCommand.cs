namespace Basalt.Core.Commands.Vanilla;

using Basalt.Core.Plugins;

public static class PluginsCommand
{
    public static readonly CommandDefinition Definition = new()
    {
        Name = "plugins",
        Description = "List loaded plugins.",
        Permissions = ["basalt.op"],
        Overloads = [new OverloadDefinition { Parameters = [] }],
        Handler = new CommandHandler(Execute)
    };

    static CommandResult Execute(CommandContext ctx)
    {
        PluginContainer[] plugins = ctx.Server.Plugins.Plugins
            .OrderBy(plugin => plugin.Description.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (plugins.Length == 0)
            return CommandResult.OkMessage("§r§7Plugins (§a0§7)\n§7` No plugins loaded.");

        string message = $"§r§7Plugins (§a{plugins.Length}§7)\n";
        for (int i = 0; i < plugins.Length; i++)
        {
            PluginDescription description = plugins[i].Description;
            string authors = description.Authors.Length == 0
                ? "Unknown"
                : string.Join(", ", description.Authors);

            message += $"§7` §a{description.Name} §7v§a{description.Version} §7by §a{authors}§7\n";
        }

        return CommandResult.OkMessage(message);
    }
}
