using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Enchantments;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class EnchantCommand : Command {
    public EnchantCommand(EnchantmentRegistry enchantments)
        : base("enchant", "Enchant a player's held item.", "basalt.command.enchant") {
        AddOverload(
            new("target", new TargetEnum(playersOnly: true)),
            new("enchantment", new EnchantmentEnum(enchantments)),
            new("level", new IntEnum(1, short.MaxValue), optional: true)
        );
    }

    public override CommandResult Execute(CommandContext context) {
        Entity[] targets = context.Get<Entity[]>("target");
        Enchantment enchantment = context.Get<Enchantment>("enchantment");
        int level = context.Get("level", 1);
        if (targets.Length is 0 or > 128)
            return new(false, "Select between 1 and 128 players.");

        int enchanted = 0;
        foreach (Entity entity in targets) {
            if (entity is not Player { Spawned: true, Disposed: false } player ||
                player.GetTrait<PlayerInventoryTrait>() is not { } inventory)
                continue;
            using Lock.Scope tickSync = player.TickSync.EnterScope();
            using Lock.Scope sync = player.Sync.EnterScope();
            ItemStack? item = inventory.Inventory.GetItem(inventory.SelectedSlot);
            if (item is null || !enchantment.AppliesTo(item.Type))
                continue;
            bool incompatible = false;
            foreach (string identifier in enchantment.IncompatibleWith.Span)
                incompatible |= item.GetEnchantmentLevel(identifier) > 0;
            if (incompatible)
                continue;

            item.AddEnchantment(enchantment, level);
            enchanted++;
        }

        return enchanted == 0
            ? new(false, "No selected player held an item that can receive this enchantment.")
            : new(true, $"Enchanted {enchanted} player item(s) with {enchantment.Identifier} {level}.");
    }
}
