namespace Basalt.Core.Commands.Vanilla;

using Basalt.Core.Worlds.Dimensions;
using BedrockProtocol.Types;
using Player = Player.Player;

public static class SetWorldSpawnCommand {
    public static readonly CommandDefinition Definition = new() {
        Name = "setworldspawn",
        Description = "Sets the world spawn position.",
        Permissions = ["basalt.op"],
        Overloads =
      [
        // /setworldspawn <position: x y z>
        new OverloadDefinition
      {
        Parameters =
        [
          new ParameterDefinition { Name = "position", Type = typeof(PositionEnum) }
        ]
      },
      // /setworldspawn (uses player's current position)
      new OverloadDefinition
      {
        Parameters = []
      }
      ],
        Handler = new CommandHandler(Execute)
    };

    static CommandResult Execute(CommandContext ctx) {
        Player? player = ctx.RequirePlayer(out CommandResult? err);
        if (player is null) return err!;

        Dimension? dimension = player.Dimension;
        if (dimension is null)
            return CommandResult.Error("No dimension available.");

        PositionEnum? position = ctx.Get<PositionEnum>("position");
        Vec3 spawn = position?.Value ?? player.Location;

        dimension.SpawnPosition = spawn;
        if (dimension.World is { } world) {
            world.Persistence.SaveSpawnPosition(dimension.Type, spawn);
            world.Persistence.Flush();
            world.Provider.WriteLevelDat(world);
        }

        int x = (int)spawn.X;
        int y = (int)spawn.Y;
        int z = (int)spawn.Z;
        return CommandResult.OkMessage($"§7Set world spawn to §a{x} {y} {z}§7.");
    }
}
