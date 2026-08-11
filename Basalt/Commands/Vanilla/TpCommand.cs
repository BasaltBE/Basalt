namespace Basalt.Core.Commands.Vanilla;

using Basalt.Core.Worlds.Dimensions;
using Player = Player.Player;

public static class TpCommand {
    public static readonly CommandDefinition Definition = new() {
        Name = "tp",
        Description = "Teleports a player to a position or another player.",
        Aliases = ["teleport"],
        Permissions = ["basalt.op"],
        Overloads =
        [
            // /tp <destination: player>
            new OverloadDefinition
            {
                Parameters =
                [
                    new ParameterDefinition { Name = "destination", Type = typeof(TargetEnum) }
                ]
            },
            // /tp <victim> <destination: player>
            new OverloadDefinition
            {
                Parameters =
                [
                    new ParameterDefinition { Name = "victim", Type = typeof(TargetEnum) },
                    new ParameterDefinition { Name = "destination", Type = typeof(TargetEnum) }
                ]
            },
            // /tp <position: x y z>
            new OverloadDefinition
            {
                Parameters =
                [
                    new ParameterDefinition { Name = "position", Type = typeof(PositionEnum) }
                ]
            },
            // /tp <victim> <position: x y z>
            new OverloadDefinition
            {
                Parameters =
                [
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

        // /tp <x y z> or /tp <victim> <x y z>
        if (position is not null) {
            Player target;
            if (victim is not null) {
                Player? resolved = victim.GetSinglePlayer(out CommandResult? err);
                if (resolved is null) return err!;
                target = resolved;
            }
            else {
                Player? self = ctx.RequirePlayer(out CommandResult? err);
                if (self is null) return err!;
                target = self;
            }

            Dimension? dim = target.Dimension;
            target.Teleport(position.Value, dim);
            return CommandResult.OkMessage($"§7Teleported §a{target.Username} §7to §a{position.Value.X:0.##} {position.Value.Y:0.##} {position.Value.Z:0.##}§7.");
        }

        // /tp <destination> or /tp <victim> <destination>
        if (destination is not null) {
            Player? destPlayer = destination.GetSinglePlayer(out CommandResult? destErr);
            if (destPlayer is null) return destErr!;

            Player source;
            if (victim is not null) {
                Player? resolved = victim.GetSinglePlayer(out CommandResult? err);
                if (resolved is null) return err!;
                source = resolved;
            }
            else {
                Player? self = ctx.RequirePlayer(out CommandResult? err);
                if (self is null) return err!;
                source = self;
            }

            source.Teleport(destPlayer.Location, destPlayer.Dimension);
            return CommandResult.OkMessage($"§7Teleported §a{source.Username} §7to §a{destPlayer.Username}§7.");
        }

        return CommandResult.Error("Usage: /tp <destination> | /tp <x> <y> <z> | /tp <victim> <destination> | /tp <victim> <x> <y> <z>");
    }
}
