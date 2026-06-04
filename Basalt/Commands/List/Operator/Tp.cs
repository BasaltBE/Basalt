namespace Basalt.Server.Commands.List.Operator;

using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using Basalt.Server.Commands;
using Basalt.Server.World.Dimension;
using Player = global::Basalt.Server.Player.Player;

public sealed class TpCommand : Basalt.Server.Commands.Command
{
    public TpCommand() : base("tp", "Teleport entities", ["teleport"], [])
    {
        Permissions.Add("basalt.op");
        CreateOverload();

        AddDisplayOverload()
            .Set<TargetEnum>("destination", true)
            .Set<DimensionEnum>("dimension", false);

        AddDisplayOverload()
            .Set<TargetEnum>("victim", true)
            .Set<TargetEnum>("destination", true)
            .Set<DimensionEnum>("dimension", false);

        AddDisplayOverload()
            .Set<PositionEnum>("destination", true)
            .Set<DimensionEnum>("dimension", false);

        AddDisplayOverload()
            .Set<TargetEnum>("victim", true)
            .Set<PositionEnum>("destination", true)
            .Set<DimensionEnum>("dimension", false);
    }

    public override CommandResult Execute(CommandExecutionState state)
    {
        string[] tokens = state.Command.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length < 2)
        {
            return CommandResult.Message(
                "§cUsage: /tp <destination> [dimension] | /tp <x> <y> <z> [dimension] | /tp <victim> <destination> [dimension] | /tp <victim> <x> <y> <z> [dimension]",
                false);
        }

        string[] args = tokens[1..];
        CommandParsing.TryStripTrailingDimension(state.Server, args, out args, out string? explicitDimensionId);
        Player? executor = state.Executor is PlayerExecutor playerExecutor ? playerExecutor.Player : null;

        if (args.Length >= 4 && CommandParsing.TryParsePosition(args, 1, new Vec3f(), out _))
        {
            return TeleportVictimsToPosition(state, executor, explicitDimensionId, args[0], args, positionStart: 1);
        }

        if (args.Length == 3 && TryParsePositionWithContext(state, executor, args, 0, null, out Vec3f selfCoords))
        {
            if (executor is null)
            {
                return CommandResult.Message("§cYou must specify a player when running this command from console.", false);
            }

            if (!TryResolveTeleportDimension(state, executor, explicitDimensionId, out Dimension? dimension, out CommandResult? error))
            {
                return error!;
            }

            return TeleportPlayers([executor], selfCoords, dimension, state, isSelf: true);
        }

        if (args.Length == 2)
        {
            return TeleportVictimsToPlayer(state, executor, explicitDimensionId, args[0], args[1]);
        }

        if (args.Length == 1)
        {
            return TeleportExecutorToPlayer(state, executor, explicitDimensionId, args[0]);
        }

        return CommandResult.Message("§cCould not parse teleport arguments.", false);
    }

    static CommandResult TeleportExecutorToPlayer(
        CommandExecutionState state,
        Player? executor,
        string? explicitDimensionId,
        string destinationToken)
    {
        if (executor is null)
        {
            return CommandResult.Message("§cYou must specify a player when running this command from console.", false);
        }

        if (destinationToken == "@a")
        {
            return CommandResult.Message("§cMultiple entities matched the target selector, please be more specific.", false);
        }

        List<Player> destinations = CommandParsing.ResolvePlayers(state.Server, executor, destinationToken);
        if (destinations.Count == 0)
        {
            return CommandResult.Message("§cNo online players matched the target selector.", false);
        }

        if (destinations.Count > 1)
        {
            return CommandResult.Message("§cMultiple entities matched the target selector, please be more specific.", false);
        }

        Player destination = destinations[0];

        if (!TryResolveTeleportDimension(state, executor, explicitDimensionId, out Dimension? dimension, out CommandResult? error))
        {
            return error!;
        }

        return TeleportPlayers([executor], destination.Position, dimension, state, isSelf: true);
    }

    static CommandResult TeleportVictimsToPlayer(
        CommandExecutionState state,
        Player? executor,
        string? explicitDimensionId,
        string victimToken,
        string destinationToken)
    {
        if (destinationToken == "@a")
        {
            return CommandResult.Message("§cMultiple entities matched the destination selector, please be more specific.", false);
        }

        List<Player> victims = CommandParsing.ResolvePlayers(state.Server, executor, victimToken);
        if (victims.Count == 0)
        {
            return CommandResult.Message("§cNo online players matched the victim selector.", false);
        }

        List<Player> destinations = CommandParsing.ResolvePlayers(state.Server, executor, destinationToken);
        if (destinations.Count == 0)
        {
            return CommandResult.Message("§cNo online players matched the destination selector.", false);
        }

        if (destinations.Count > 1)
        {
            return CommandResult.Message("§cMultiple entities matched the destination selector, please be more specific.", false);
        }

        Player destination = destinations[0];

        if (!TryResolveTeleportDimension(state, executor, explicitDimensionId, out Dimension? dimension, out CommandResult? error))
        {
            return error!;
        }

        return TeleportPlayers(victims, destination.Position, dimension, state, isSelf: false, destinationName: destination.Username);
    }

    static CommandResult TeleportVictimsToPosition(
        CommandExecutionState state,
        Player? executor,
        string? explicitDimensionId,
        string victimToken,
        string[] args,
        int positionStart)
    {
        List<Player> victims = CommandParsing.ResolvePlayers(state.Server, executor, victimToken);
        if (victims.Count == 0)
        {
            return CommandResult.Message("§cNo online players matched the victim selector.", false);
        }

        Player? originPlayer = victims[0];
        if (!TryParsePositionWithContext(state, executor, args, positionStart, originPlayer, out Vec3f position))
        {
            return CommandResult.Message("§cInvalid coordinates.", false);
        }

        if (!TryResolveTeleportDimension(state, executor, explicitDimensionId, out Dimension? dimension, out CommandResult? error))
        {
            return error!;
        }

        return TeleportPlayers(victims, position, dimension, state, isSelf: false);
    }

    static bool TryResolveTeleportDimension(
        CommandExecutionState state,
        Player? executor,
        string? explicitDimensionId,
        out Dimension? dimension,
        out CommandResult? error)
    {
        dimension = null;
        error = null;

        if (!string.IsNullOrWhiteSpace(explicitDimensionId))
        {
            if (!CommandParsing.TryFindRegisteredDimension(state.Server, explicitDimensionId, out dimension) || dimension is null)
            {
                error = CommandResult.Message($"§cCould not find dimension '{explicitDimensionId}'.", false);
                return false;
            }

            return true;
        }

        dimension = executor?.Dimension;
        if (dimension is null)
        {
            dimension = state.Server.GetWorld().GetDimension(DimensionType.Overworld);
        }

        if (dimension is null)
        {
            error = CommandResult.Message("§cCould not resolve a dimension for teleport.", false);
            return false;
        }

        return true;
    }

    static bool TryParsePositionWithContext(
        CommandExecutionState state,
        Player? executor,
        string[] args,
        int start,
        Player? originPlayer,
        out Vec3f position)
    {
        Vec3f origin = originPlayer?.Position ?? executor?.Position ?? new Vec3f();
        return CommandParsing.TryParsePosition(args, start, origin, out position);
    }

    static CommandResult TeleportPlayers(
        List<Player> players,
        Vec3f position,
        Dimension? dimension,
        CommandExecutionState state,
        bool isSelf,
        string? destinationName = null)
    {
        if (dimension is null)
        {
            return CommandResult.Message("§cCould not resolve a dimension for teleport.", false);
        }

        List<string> messages = [];
        int successCount = 0;

        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            try
            {
                player.Teleport(position, dimension);
                successCount++;

                if (isSelf && ReferenceEquals((state.Executor as PlayerExecutor)?.Player, player))
                {
                    if (destinationName is not null)
                    {
                        messages.Add($"§7Teleported you to §a{destinationName}§7.");
                    }
                    else
                    {
                        messages.Add($"§7Teleported you to §a{position.X:0.##} {position.Y:0.##} {position.Z:0.##}§7.");
                    }
                }
                else if (destinationName is not null)
                {
                    messages.Add($"§7Teleported §a{player.Username} §7to §a{destinationName}§7.");
                    player.SendMessage($"§7You were teleported to §a{destinationName}§7.");
                }
                else
                {
                    messages.Add($"§7Teleported §a{player.Username} §7to §a{position.X:0.##} {position.Y:0.##} {position.Z:0.##}§7.");
                    player.SendMessage($"§7You were teleported to §a{position.X:0.##} {position.Y:0.##} {position.Z:0.##}§7.");
                }
            }
            catch (Exception exception)
            {
                messages.Add($"§cCould not teleport §a{player.Username}§c: {exception.Message}");
            }
        }

        if (successCount == 0)
        {
            return new CommandResult
            {
                Success = false,
                Messages = messages.Count == 0 ? ["§cNo players were teleported."] : messages
            };
        }

        return new CommandResult
        {
            Success = true,
            Messages = messages
        };
    }
}
