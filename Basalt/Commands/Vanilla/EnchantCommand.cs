namespace Basalt.Core.Commands.Vanilla;

using Basalt.Core.Entities.Traits;
using Basalt.Core.Item;
using Basalt.Core.Item.Enchantment;
using Basalt.Core.Item.Traits;
using Player = Player.Player;

public static class EnchantCommand
{
    public static readonly CommandDefinition Definition = new()
    {
        Name = "enchant",
        Description = "Enchants the held item of a player.",
        Permissions = ["basalt.op"],
        Overloads =
      [
        // /enchant <player> <enchantment> [level]
        new OverloadDefinition
      {
        Parameters =
        [
          new ParameterDefinition { Name = "player", Type = typeof(TargetEnum) },
          new ParameterDefinition { Name = "enchantment", Type = typeof(EnchantmentEnum) },
          new ParameterDefinition { Name = "level", Type = typeof(IntEnum), Optional = true }
        ]
      }
      ],
        Handler = new CommandHandler(Execute)
    };

    private static CommandResult Execute(CommandContext ctx)
    {
        TargetEnum? target = ctx.Get<TargetEnum>("player");
        EnchantmentEnum? enchantArg = ctx.Get<EnchantmentEnum>("enchantment");
        IntEnum? levelArg = ctx.Get<IntEnum>("level");

        if (target is null || enchantArg is null || enchantArg.Type is null)
            return CommandResult.Error("Usage: /enchant <player> <enchantment> [level]");

        EnchantmentType enchantment = enchantArg.Type;
        int level = levelArg?.Value ?? 1;

        if (level < 1 || level > enchantment.MaxLevel)
            return CommandResult.Error($"Level must be between 1 and {enchantment.MaxLevel}.");

        List<Player> players = target.GetPlayers();
        if (players.Count == 0)
            return CommandResult.Error("No player found matching the target selector.");

        int enchanted = 0;
        foreach (Player player in players)
        {
            if (EnchantHeldItem(player, enchantment, level))
                enchanted++;
        }

        if (enchanted == 0)
            return CommandResult.Error("Target has no item in hand.");

        if (players.Count == 1)
            return CommandResult.OkMessage($"§7Applied §a{enchantment.Identifier} {level} §7to §a{players[0].Username}§7's held item.");

        return CommandResult.OkMessage($"§7Applied §a{enchantment.Identifier} {level} §7to §a{enchanted} players§7' held items.");
    }

    private static bool EnchantHeldItem(Player player, EnchantmentType enchantment, int level)
    {
        EntityInventoryTrait? inventory = player.GetTrait<EntityInventoryTrait>();
        if (inventory is null) return false;

        ItemStack? held = inventory.GetHeldItem();
        if (held is null || held.Type.NetworkId == 0) return false;

        ItemStackEnchantmentTrait? trait = held.GetTrait<ItemStackEnchantmentTrait>();
        if (trait is null)
        {
            trait = held.AddTrait(new ItemStackEnchantmentTrait(held));
        }

        trait.AddEnchantment(new EnchantmentInstance(enchantment, level));
        inventory.Container.UpdateSlot(inventory.SelectedSlot);
        return true;
    }
}
