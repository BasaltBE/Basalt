namespace Basalt.Core.Commands.Vanilla;

using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits;
using Player = Player.Player;

public static class ClearCommand {
    public static readonly CommandDefinition Definition = new() {
        Name = "clear",
        Description = "Clear a player's inventory.",
        Permissions = ["basalt.op"],
        Overloads =
        [
            // /clear
            new OverloadDefinition { Parameters = [] },
            // /clear <target>
            new OverloadDefinition
            {
                Parameters =
                [
                    new ParameterDefinition { Name = "target", Type = typeof(TargetEnum), Optional = true }
                ]
            }
        ],
        Handler = new CommandHandler(Execute)
    };

    static CommandResult Execute(CommandContext ctx) {
        TargetEnum? target = ctx.Get<TargetEnum>("target");

        Entity entity;
        if (target is not null) {
            if (target.Entities.Length == 0)
                return CommandResult.Error("No entities matched the target selector.");
            entity = target.Entities[0];
        }
        else {
            Player? self = ctx.RequirePlayer(out CommandResult? error);
            if (self is null) return error!;
            entity = self;
        }

        EntityInventoryTrait? inventory = entity.GetTrait<EntityInventoryTrait>();
        if (inventory is null)
            return CommandResult.Ok;

        int size = inventory.Container.Storage?.Sum(item => item?.StackSize ?? 0) ?? 0;
        inventory.Clear();

        string name = entity is Player p ? p.Username : entity.FormatIdentifier();
        return CommandResult.OkMessage($"§7Cleared §a{size} §7items from §a{name}'s §7inventory.");
    }
}
