namespace Basalt.Core.Commands.Vanilla;

using Basalt.Core.Blocks;
using Basalt.Core.Worlds.Dimensions;
using Basalt.Protocol.Types;
using Player = Player.Player;

public static class SetBlockCommand {
  public static readonly CommandDefinition Definition = new() {
    Name = "setblock",
    Description = "Sets a block at a given position.",
    Permissions = ["basalt.op"],
    Overloads =
    [
      new OverloadDefinition
      {
        Parameters =
        [
          new ParameterDefinition { Name = "position", Type = typeof(PositionEnum) },
          new ParameterDefinition { Name = "block", Type = typeof(BlockEnum) }
        ]
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
    BlockEnum? block = ctx.Get<BlockEnum>("block");

    if (position is null || block is null)
      return CommandResult.Error("Usage: /setblock <position: x y z> <block>");

    int x = (int)MathF.Floor(position.Value.X);
    int y = (int)MathF.Floor(position.Value.Y);
    int z = (int)MathF.Floor(position.Value.Z);

    BlockPermutation permutation = block.Type.GetPermutation();
    dimension.SetPermutation(x, y, z, permutation);

    return CommandResult.OkMessage($"§7Set block at §a{x} {y} {z}§7 to §a{block.Type.Identifier}§7.");
  }
}
