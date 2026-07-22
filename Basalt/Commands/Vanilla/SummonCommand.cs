namespace Basalt.Core.Commands.Vanilla;

using Basalt.Core.Entities;
using Basalt.Core.Entities.Traits.Types;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using Player = Player.Player;

public static class SummonCommand {
    public static readonly CommandDefinition Definition = new() {
        Name = "summon",
        Description = "Summon an entity.",
        Permissions = ["basalt.op"],
        Overloads =
        [
            // /summon <entity>
            new OverloadDefinition
            {
                Parameters =
                [
                    new ParameterDefinition { Name = "entity", Type = typeof(EntityEnum) }
                ]
            },
            // /summon <entity> <position>
            new OverloadDefinition
            {
                Parameters =
                [
                    new ParameterDefinition { Name = "entity", Type = typeof(EntityEnum) },
                    new ParameterDefinition { Name = "position", Type = typeof(PositionEnum), Optional = true }
                ]
            }
        ],
        Handler = new CommandHandler(Execute)
    };

    static CommandResult Execute(CommandContext ctx) {
        EntityEnum? entityArg = ctx.Get<EntityEnum>("entity");
        if (entityArg is null || string.IsNullOrWhiteSpace(entityArg.EntityIdentifier))
            return CommandResult.Error("Usage: /summon <entity> [x y z]");

        string identifier = entityArg.EntityIdentifier;

        // Resolve position
        PositionEnum? posArg = ctx.Get<PositionEnum>("position");
        Vec3f position;
        Dimension? dimension;

        if (posArg is not null) {
            position = posArg.Value;
            Player? self = ctx.Sender.AsPlayer();
            dimension = self?.Dimension ?? ctx.Server.GetWorld().GetDimension(DimensionType.Overworld);
        }
        else {
            Player? self = ctx.RequirePlayer(out CommandResult? error);
            if (self is null) return error!;
            position = self.Location;
            dimension = self.Dimension;
        }

        if (dimension is null)
            return CommandResult.Error("Could not resolve a dimension for spawning.");

        Entity entity;
        try {
            entity = new Entity(identifier);
        }
        catch (Exception ex) {
            return CommandResult.Error($"Could not create entity '{identifier}': {ex.Message}");
        }

        entity.Location = position;
        entity.Spawn(dimension, new EntitySpawnOptions(InitialSpawn: false));

        return CommandResult.OkMessage($"§7Summoned §a{entity.FormatIdentifier()} §7at §a{position.X:0.##} {position.Y:0.##} {position.Z:0.##}§7.");
    }
}
