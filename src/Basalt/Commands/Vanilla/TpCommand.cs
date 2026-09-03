namespace Basalt.Core.Commands.Vanilla;

using Basalt.Core.Entities;
using Basalt.Core.Worlds.Dimensions;
using Player = Player.Player;

public static class TpCommand {
    public static readonly CommandDefinition Definition = new() {
        Name = "tp",
        Description = "Teleports an entity to a position or another entity.",
        Aliases = ["teleport"],
        Permissions = ["basalt.op"],
        Overloads =
        [
            new OverloadDefinition {
                Parameters = [new ParameterDefinition { Name = "destination", Type = typeof(TargetEnum) }]
            },
            new OverloadDefinition {
                Parameters = [
                    new ParameterDefinition { Name = "victim", Type = typeof(TargetEnum) },
                    new ParameterDefinition { Name = "destination", Type = typeof(TargetEnum) }
                ]
            },
            new OverloadDefinition {
                Parameters = [new ParameterDefinition { Name = "position", Type = typeof(PositionEnum) }]
            },
            new OverloadDefinition {
                Parameters = [
                    new ParameterDefinition { Name = "victim", Type = typeof(TargetEnum) },
                    new ParameterDefinition { Name = "position", Type = typeof(PositionEnum) }
                ]
            }
        ],
        Handler = new CommandHandler(Execute)
    };

    static CommandResult Execute(CommandContext ctx) {
        PositionEnum? position = ctx.Get<PositionEnum>("position");
        TargetEnum? victim = ctx.Get<TargetEnum>("victim");
        TargetEnum? destination = ctx.Get<TargetEnum>("destination");

        if (position is not null) {
            Entity[] targets;
            if (victim is not null) {
                targets = victim.Entities;
            }
            else {
                Player? self = ctx.RequirePlayer(out CommandResult? error);
                if (self is null) return error!;
                targets = [self];
            }

            if (targets.Length == 0)
                return CommandResult.Error("No entities matched the target selector.");

            int teleported = 0;
            for (int i = 0; i < targets.Length; i++) {
                Entity target = targets[i];
                if (ctx.QueueOnOwner(target, () => target.Teleport(position.Value, target.Dimension)))
                    teleported++;
            }

            return CommandResult.OkMessage($"Teleported {teleported} entities to {position.Value.X:0.##} {position.Value.Y:0.##} {position.Value.Z:0.##}.");
        }

        if (destination is not null) {
            Entity? target = destination.GetSingleEntity(out CommandResult? destinationError);
            if (target is null)
                return destinationError!;

            Entity[] sources;
            if (victim is not null) {
                sources = victim.Entities;
            }
            else {
                Player? self = ctx.RequirePlayer(out CommandResult? error);
                if (self is null) return error!;
                sources = [self];
            }

            if (sources.Length == 0)
                return CommandResult.Error("No entities matched the target selector.");

            int teleported = 0;
            for (int i = 0; i < sources.Length; i++) {
                Entity source = sources[i];
                if (source.Dimension != target.Dimension)
                    continue;

                if (ctx.QueueOnOwner(source, () => source.Teleport(target.Location, target.Dimension)))
                    teleported++;
            }

            return CommandResult.OkMessage($"Teleported {teleported} entities to {target.Identifier}.");
        }

        return CommandResult.Error("Usage: /tp <destination> | /tp <x> <y> <z> | /tp <victim> <destination> | /tp <victim> <x> <y> <z>");
    }
}
