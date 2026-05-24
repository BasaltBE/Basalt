using Basalt.Commands;

public class GiveCommand : Command
{
    public GiveCommand() : base("give", "Give an item to a player.")
    {
        CreateOverload()
            .Set<TargetEnum>("player", true)
            .Set<ItemEnum>("itemName", true)
            .Set<IntEnum>("amount", false)
            .Set<IntEnum>("data", false)
            .Set<JsonEnum>("components", false);
    }

    public override CommandResult Execute(CommandExecutionState state)
    {
        return CommandResult.Message("This command is not implemented yet.", true);
    }
}
