namespace Basalt.Core.Commands.List.Operator;

using Basalt.Core.Commands;
using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits;
using Player = Player.Player;


public class ClearCommand : Command
{
    public ClearCommand() : base("clear", "Clear an inventory")
    {
        Permissions.Add("basalt.op");

        CreateOverload()
            .Set<TargetEnum>("target", false);
    }

    public override CommandResult Execute(CommandExecutionState state)
    {
        var target = state.Get<TargetEnum>("target");

        if (target != null)
            return ClearTargetInventory(target);

        if (state.Executor is PlayerExecutor executor)
            return ClearEntityInventory(executor.Player);

        return CommandResult.Empty(true);
    }

    private static CommandResult ClearTargetInventory(TargetEnum target)
    {
        var entity = target.Entities.FirstOrDefault();

        if (entity == null)
            return CommandResult.Message("No entities matched the target selector", false);

        return ClearEntityInventory(entity);
    }

    private static CommandResult ClearEntityInventory(Entity entity)
    {
        var inventory = entity.GetTrait<EntityInventoryTrait>();

        if (inventory == null)
            return CommandResult.Empty(true);

        var size = inventory.Container.Storage?.Sum(item => item?.StackSize ?? 0) ?? 0;
        inventory.Clear();

        if (entity is Player player)
        {
            player.SendMessage("§7Your inventory has been cleared.");
            return CommandResult.Message($"§7Cleared §a{size} §7items from §a{player.Username}'s inventory", true);
        }

        var name = entity.FormatIdentifier();
        return CommandResult.Message($"§7Cleared §a{size} §7items from §a{name}'s inventory", true);
    }
}






