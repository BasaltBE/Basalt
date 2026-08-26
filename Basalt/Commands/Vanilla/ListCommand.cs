namespace Basalt.Core.Commands.Vanilla;

using Player = Basalt.Core.Player.Player;

public static class ListCommand {
    public static readonly CommandDefinition Definition = new() {
        Name = "list",
        Description = "Get a list of players on the server.",
        Overloads = [new OverloadDefinition { Parameters = [] }],
        Handler = new CommandHandler(Execute)
    };

    static CommandResult Execute(CommandContext ctx) {
        Player[] players = ctx.Server.GetPlayersSnapshot();
        int count = players.Length;

        string message = $"§r§7There are (§a{count}§7) Players Online.";
        if (count > 0) {
            string names = string.Join(", ", players.Select(p => $"§a{p.Username}"));
            message += $"\n{names}";
        }

        return CommandResult.OkMessage(message);
    }
}
