namespace Basalt.Core.Commands.Vanilla;

public static class StopCommand
{
    public static readonly CommandDefinition Definition = new()
    {
        Name = "stop",
        Description = "Stops the server.",
        Permissions = ["basalt.op"],
        Overloads = [new OverloadDefinition { Parameters = [] }],
        Handler = new CommandHandler(Execute)
    };

    static CommandResult Execute(CommandContext ctx)
    {
        ctx.Server.Stop();
        return CommandResult.OkMessage("Stopping server...");
    }
}
