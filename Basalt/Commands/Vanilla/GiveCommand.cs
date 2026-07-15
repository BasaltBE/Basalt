namespace Basalt.Core.Commands.Vanilla;

using Basalt.Core.Entities.Traits;
using Basalt.Core.Item;
using Player = Player.Player;

public static class GiveCommand
{
    public static readonly CommandDefinition Definition = new()
    {
        Name = "give",
        Description = "Gives an item to a player.",
        Permissions = ["basalt.op"],
        Overloads =
        [
            // /give <player> <item>
            new OverloadDefinition
            {
                Parameters =
                [
                    new ParameterDefinition { Name = "player", Type = typeof(TargetEnum) },
                    new ParameterDefinition { Name = "item", Type = typeof(ItemEnum) }
                ]
            },
            // /give <player> <item> <count>
            new OverloadDefinition
            {
                Parameters =
                [
                    new ParameterDefinition { Name = "player", Type = typeof(TargetEnum) },
                    new ParameterDefinition { Name = "item", Type = typeof(ItemEnum) },
                    new ParameterDefinition { Name = "amount", Type = typeof(IntEnum), Optional = true }
                ]
            }
        ],
        Handler = new CommandHandler(Execute)
    };

    static CommandResult Execute(CommandContext ctx)
    {
        TargetEnum? target = ctx.Get<TargetEnum>("player");
        ItemEnum? item = ctx.Get<ItemEnum>("item");
        IntEnum? amountArg = ctx.Get<IntEnum>("amount");
        int amount = amountArg?.Value ?? 1;

        if (target is null || item is null)
            return CommandResult.Error("Usage: /give <player> <item> [amount]");

        if (amount <= 0 || amount > 255)
            return CommandResult.Error("Amount must be between 1 and 255.");

        List<Player> players = target.GetPlayers();
        if (players.Count == 0)
            return CommandResult.Error("No player found matching the target selector.");

        int totalGiven = 0;
        foreach (Player player in players)
        {
            totalGiven += GiveToPlayer(player, item.Type, amount);
        }

        if (totalGiven == 0)
            return CommandResult.Error("Player's inventory is full.");

        if (players.Count == 1)
            return CommandResult.OkMessage($"§7Given §a{totalGiven} §7of §a{item.Raw} §7to §a{players[0].Username}§7.");

        return CommandResult.OkMessage($"§7Given §a{totalGiven} §7of §a{item.Raw} §7to §a{players.Count} players§7.");
    }

    static int GiveToPlayer(Player player, ItemType type, int amount)
    {
        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (inventory is null)
            return 0;

        int given = 0;
        int remaining = amount;
        while (remaining > 0)
        {
            int toGive = Math.Min(type.MaxStackSize, remaining);
            ItemStack stack = new(type, (ushort)toGive);
            if (!inventory.Container.AddItem(stack))
                break;

            given += toGive;
            remaining -= toGive;
        }
        return given;
    }
}
