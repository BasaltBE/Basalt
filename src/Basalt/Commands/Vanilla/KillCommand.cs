namespace Basalt.Core.Commands.Vanilla;

using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Entities.Traits.Types;
using Basalt.BedrockProtocol.Enums;
using Player = Player.Player;

public static class KillCommand {
    public static readonly CommandDefinition Definition = new() {
        Name = "kill",
        Description = "Kills entities.",
        Permissions = ["basalt.op"],
        Overloads =
        [
            new OverloadDefinition {
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
        Entity[] entities;

        if (target is not null) {
            entities = target.Entities;
        }
        else {
            Player? self = ctx.RequirePlayer(out CommandResult? error);
            if (self is null) return error!;
            entities = [self];
        }

        if (entities.Length == 0)
            return CommandResult.Error("No entities matched the target selector.");

        for (int i = 0; i < entities.Length; i++) {
            Entity entity = entities[i];
            ctx.QueueOnOwner(entity, () => {
                if (entity is Player player) {
                    EntityHealthTrait? health = player.GetTrait<EntityHealthTrait>();
                    health?.ApplyDamage(MathF.Max(health.CurrentValue, 1f), null, ActorDamageCause.SelfDestruct);
                }
                else {
                    entity.Kill(new EntityDeathOptions(DamageCause: ActorDamageCause.SelfDestruct));
                }
            });
        }

        string suffix = entities.Length == 1 ? "y" : "ies";
        return CommandResult.OkMessage($"Killed {entities.Length} entit{suffix}.");
    }
}
