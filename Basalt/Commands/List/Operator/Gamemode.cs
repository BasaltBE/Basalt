using Basalt.Commands;
using Basalt.Core;
using Basalt.Protocol.Enums;

public class GamemodeEnum : CustomEnum
{
    public static readonly string[] Values =
    [
        "survival",
        "creative",
        "adventure",
        "spectator"
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
                gm = Gamemode.Survival;
                break;
            case "creative":
                gm = Gamemode.Creative;
                break;
            case "adventure":
                gm = Gamemode.Adventure;
                break;
            case "spectator":
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
            else
            {
                return CommandResult.Message("§cYou must specify a target, or be a player!", false);
            }
        }
        else
        {
            var Entities = target.Entities;
            if (Entities.Length == 0)
                return CommandResult.Message("§cNo entities matched the target selector", false);
            if (Entities.Length > 1)
                return CommandResult.Message("§cMultiple entities matched the target selector, please be more specific", false);

            var entity = Entities[0];
            if (entity is Player player)
            {
                player.SetGamemode(gm);
                player.SendMessage("§7Your game mode has been changed to §a" + gamemode?.Value);
                return CommandResult.Message($"§7Set §a{player.Username}'s §7game mode to §a{gamemode?.Value}§7.", true);
            }
            else
            {
                return CommandResult.Message("§cThe target selector must be a player!", false);
            }
        }

        // return CommandResult.Empty(true);
    }
}
