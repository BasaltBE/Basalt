using Basalt.Commands;
using Basalt.Core;
using Basalt.Protocol.Enums;

public class GamemodeEnum : CustomEnum
{
    public static readonly string[] Values =
    [
        "survival",
        "s",
        "0",
        "creative",
        "c",
        "1",
        "adventure",
        "a",
        "2",
        "spectator",
        "sp",
        "6"
    ];

    public GamemodeEnum() : base("gamemode") { }
}

public class GamemodeCommand : Command
{
    public GamemodeCommand() : base("gamemode", "Change the game mode of a player")
    {
        Permissions.Add("basalt.op");

        CreateOverload()
            .Set<GamemodeEnum>("gamemode", true)
            .Set<TargetEnum>("target", false);
    }

    public override CommandResult Execute(CommandExecutionState state)
    {
        var gamemode = state.Get<GamemodeEnum>("gamemode");
        var target = state.Get<TargetEnum>("target");

        var gm = Gamemode.Survival;
        switch (gamemode?.Value)
        {
            case "survival":
            case "s":
            case "0":
                gm = Gamemode.Survival;
                break;
            case "creative":
            case "c":
            case "1":
                gm = Gamemode.Creative;
                break;
            case "adventure":
            case "a":
            case "2":
                gm = Gamemode.Adventure;
                break;
            case "spectator":
            case "sp":
            case "6":
                gm = Gamemode.Spectator;
                break;
        }

        if (target == null)
        {
            if (state.Executor is PlayerExecutor executor)
            {
                executor.Player.SetGamemode(gm);
                return CommandResult.Message("§7Your game mode has been changed to §a" + gamemode?.Value, true);
            }

            return CommandResult.Message("§cYou must specify a target, or be a player!", false);
        }

        if (target.Entities.Length > 1 || target.OfflineUsernames.Length > 1)
        {
            return CommandResult.Message("§cMultiple entities matched the target selector, please be more specific", false);
        }

        if (target.Entities.Length == 1)
        {
            if (target.Entities[0] is Player player)
            {
                player.SetGamemode(gm);
                player.SendMessage("§7Your game mode has been changed to §a" + gamemode?.Value);
                return CommandResult.Message($"§7Set §a{player.Username}'s §7game mode to §a{gamemode?.Value}§7.", true);
            }

            return CommandResult.Message("§cThe target selector must be a player!", false);
        }

        if (target.OfflineUsernames.Length == 1)
        {
            string username = target.OfflineUsernames[0];
            if (!OfflinePlayerActions.TrySetGamemode(state.Server.GetWorld(), username, gm))
            {
                return CommandResult.Message("§cNo players matched the target selector", false);
            }

            return CommandResult.Message($"§7Set §a{username}'s §7game mode to §a{gamemode?.Value}§7.", true);
        }

        return CommandResult.Message("§cNo entities matched the target selector", false);
    }
}
