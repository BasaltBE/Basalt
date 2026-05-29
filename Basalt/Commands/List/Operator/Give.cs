using Basalt.Commands;
using Basalt.Core;
using Basalt.Entity.Traits;
using Basalt.Item;

public class GiveCommand : Command
{
    public GiveCommand() : base("give", "Give an item to a player.")
    {
        Permissions.Add("basalt.op");

        CreateOverload()
            .Set<TargetEnum>("player", true)
            .Set<ItemEnum>("item", true) // Bedrock uses itemName but cant get it to work so its for later
            .Set<IntEnum>("amount", false);
            // .Set<IntEnum>("data", false) // TODO
            // .Set<JsonEnum>("components", false); // TODO
    }

    public override CommandResult Execute(CommandExecutionState state)
    {
        var target = state.Get<TargetEnum>("player");
        var item = state.Get<ItemEnum>("item");
        var amount = state.Get<IntEnum>("amount")?.Value ?? 1;

        if (target == null || item == null)
        {
            return CommandResult.Empty(false);
        }

        if (target.Entities.Length == 0)
        {
            if (target.OfflineUsernames.Length > 0)
            {
                return CommandResult.Message("§cThat player must be online to receive items.", false);
            }

            return CommandResult.Message("§cNo entities matched the target selector", false);
        }

        if (amount <= 0)
        {
            return CommandResult.Message("§cThe amount must be greater than 0", false);
        }

        List<Player> players = [];
        for (int i = 0; i < target.Entities.Length; i++)
        {
            if (target.Entities[i] is Player player)
            {
                players.Add(player);
            }
        }

        if (players.Count == 0)
        {
            return CommandResult.Message("§cThe target selector must be a player!", false);
        }

        int successCount = 0;
        List<string> messages = [];

        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            int given = GiveItem(player, item.Type, amount);
            if (given == 0)
            {
                messages.Add($"§c{player.Username}'s inventory is full.");
                continue;
            }

            successCount++;

            if (ReferenceEquals((state.Executor as PlayerExecutor)?.Player, player))
            {
                messages.Add($"§7Given §a{given} §7of §a{item.Raw} §7to you.");
            }
            else
            {
                player.SendMessage($"§7You were given §a{given} §7of §a{item.Raw}§7.");
                messages.Add($"§7Given §a{given} §7of §a{item.Raw} §7to §a{player.Username}§7.");
            }

        }

        if (successCount == 0)
        {
            return new CommandResult
            {
                Success = false,
                Messages = messages.Count == 0 ? ["§cNo items were given."] : messages
            };
        }

        return new CommandResult
        {
            Success = true,
            Messages = messages
        };
    }

    static int GiveItem(Player player, ItemType type, int amount)
    {
        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (inventory is null)
        {
            return 0;
        }

        int given = 0;
        int remaining = amount;
        while (remaining > 0)
        {
            int count = Math.Min(type.MaxStackSize, remaining);
            ItemStack stack = new(type, (ushort)count);
            if (!inventory.Container.AddItem(stack))
            {
                break;
            }

            given += count;
            remaining -= count;
        }

        return given;
    }
}
