namespace Basalt.Server.Commands.List.Operator;

using Basalt.Server.Commands;
using Basalt.Protocol.Enums;
using Player = global::Basalt.Server.Player.Player;

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
        GamemodeEnum? gamemode = state.Get<GamemodeEnum>("gamemode");
        TargetEnum? target = state.Get<TargetEnum>("target");

        Gamemode gm = Gamemode.Survival;
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

        if (target is null)
        {
            if (state.Executor is PlayerExecutor executor)
            {
                executor.Player.SetGamemode(gm);
                return CommandResult.Message("Your game mode has been changed to " + gamemode?.Value, true);
            }

            return CommandResult.Message("You must specify a target, or be a player!", false);
        }

        if (target.Entities.Length > 1)
        {
            return CommandResult.Message("Multiple entities matched the target selector, please be more specific", false);
        }

        if (target.Entities.Length == 1)
        {
            if (target.Entities[0] is Player player)
            {
                player.SetGamemode(gm);
                player.SendMessage("Your game mode has been changed to " + gamemode?.Value);
                return CommandResult.Message($"Set {player.Username}'s game mode to {gamemode?.Value}.", true);
            }

            return CommandResult.Message("The target selector must be a player!", false);
        }

        return CommandResult.Message("No online entities matched the target selector", false);
    }
}
