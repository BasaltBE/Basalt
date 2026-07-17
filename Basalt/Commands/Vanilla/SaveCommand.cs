namespace Basalt.Core.Commands.Vanilla;

public static class SaveCommand
{
    public static readonly CommandDefinition Definition = new()
    {
        Name = "save",
        Description = "Saves all worlds to disk.",
        Aliases = ["save-all"],
        Permissions = ["basalt.op"],
        Overloads = [new OverloadDefinition { Parameters = [] }],
        Handler = new CommandHandler(Execute)
    };

    private static CommandResult Execute(CommandContext ctx)
    {
        ctx.Server.SaveAll();
        return CommandResult.OkMessage("All worlds saved.");
    }
}
