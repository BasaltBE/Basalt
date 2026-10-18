using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Items;
using BasaltBE.Basalt.Items.Types;

namespace BasaltBE.Basalt.Commands.Vanilla;

public sealed class GiveCommand : Command {
    public GiveCommand() : base("give", "Give items to players.", "basalt.command.give") {
        AddOverload(
            new("player", new TargetEnum(playersOnly: true)),
            new("item", new ItemEnum()),
            new("amount", new IntEnum(1, 32767), optional: true)
        );
    }

    public override CommandResult Execute(CommandContext context) {
        Entity[] targets = context.Get<Entity[]>("player");
        ItemType item = context.Get<ItemType>("item");
        int amount = context.Get("amount", 1);
        if (targets.Length == 0 || targets.Length > 128)
            return new(false, "Select between 1 and 128 players.");
        if (item.NetworkId == 0 || item.MaxAmount == 0)
            return new(false, "This item cannot be given.");

        int given = 0;
        int recipients = 0;
        foreach (Entity target in targets) {
            if (target is not Player { Disposed: false, Spawned: true } player)
                continue;
            using Lock.Scope tickSync = player.TickSync.EnterScope();
            using Lock.Scope sync = player.Sync.EnterScope();
            if (player.Disposed || player.GetTrait<PlayerInventoryTrait>() is not { } inventory)
                continue;
            ItemStack? leftover = inventory.Inventory.AddItem(new ItemStack(item, (ushort)amount));
            int inserted = amount - (leftover?.StackSize ?? 0);
            given += inserted;
            recipients++;
        }
        return new(recipients > 0, FormattableString.Invariant($"Gave {given} {item.Identifier} to {recipients} player(s)."));
    }
}
