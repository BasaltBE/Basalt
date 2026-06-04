namespace Basalt.Server.Commands.List.Operator;

using Basalt.Protocol.Enums;
using Basalt.Server.Commands;
using Vec3f = Basalt.Protocol.Types.Vec3f;
using Basalt.Server.World.Dimension;
using Player = global::Basalt.Server.Player.Player;
using WorldInstance = Basalt.Server.World.World;

public class TpCommand : Command
{
    public TpCommand() : base("tp", "Teleport entities", ["teleport"], [])
    {
        Permissions.Add("basalt.op");
        CreateOverload();

        AddDisplayOverload()
            .Set<TargetEnum>("destination", true)
            .Set<StringEnum>("dimension", false);

        AddDisplayOverload()
            .Set<TargetEnum>("victim", true)
            .Set<TargetEnum>("destination", true)
            .Set<StringEnum>("dimension", false);

        AddDisplayOverload()
            .Set<PositionEnum>("destination", true)
            .Set<StringEnum>("dimension", false);

        AddDisplayOverload()
            .Set<TargetEnum>("victim", true)
            .Set<PositionEnum>("destination", true)
            .Set<StringEnum>("dimension", false);
    }

    public override string? GetHelpMessage() =>
        "§cUsage: /tp <destination> [dimension] | /tp <x> <y> <z> [dimension] | /tp <victim> <destination> [dimension] | /tp <victim> <x> <y> <z> [dimension]";

    public override CommandResult? ExecuteManual(CommandExecutionState state, string[] tokens, int argumentOffset)
    {
        string[] args = tokens[argumentOffset..];
        Player? executor = GetExecutor(state);
        WorldInstance contextWorld = executor?.Dimension?.World ?? state.Server.GetWorld();

        CommandParsing.TryStripTrailingDimension(contextWorld, args, out args, out string? explicitDimensionId);

        if (args.Length >= 4 && CommandParsing.TryParsePosition(args, 1, new Vec3f(), out _))
        {
            return TeleportVictimsToPosition(state, executor, contextWorld, explicitDimensionId, args[0], args, positionStart: 1);
        }

        if (args.Length == 3 && TryParsePosition(executor, args, 0, null, out Vec3f selfCoords))
        {
            CommandResult? executorError = RequireExecutor(executor);
            if (executorError is not null)
            {
                return executorError;
            }

            Dimension? dimension = ResolveCoordsDimension(contextWorld, executor!, explicitDimensionId);
            return TeleportPlayers(state, [executor!], selfCoords, dimension, destinationName: null);
        }

        if (args.Length == 2)
        {
            return TeleportVictimsToPlayer(state, executor, contextWorld, explicitDimensionId, args[0], args[1]);
        }

        if (args.Length == 1)
        {
            return TeleportExecutorToPlayer(state, executor, contextWorld, explicitDimensionId, args[0]);
        }

        return CommandResult.Message(GetHelpMessage()!, false);
    }

    static CommandResult TeleportExecutorToPlayer(
        CommandExecutionState state,
        Player? executor,
        WorldInstance contextWorld,
        string? explicitDimensionId,
        string destinationToken)
    {
        CommandResult? executorError = RequireExecutor(executor);
        if (executorError is not null)
        {
            return executorError;
        }

        if (!TryGetSinglePlayer(state, executor, destinationToken, out Player destination, out CommandResult? targetError))
        {
            return targetError!;
        }

        Dimension? dimension = ResolvePlayerDestinationDimension(contextWorld, destination, explicitDimensionId);
        return TeleportPlayers(state, [executor!], destination.Position, dimension, destinationName: destination.Username);
    }

    static CommandResult TeleportVictimsToPlayer(
        CommandExecutionState state,
        Player? executor,
        WorldInstance contextWorld,
        string? explicitDimensionId,
        string victimToken,
        string destinationToken)
    {
        if (!TryGetPlayers(state, executor, victimToken, out List<Player> victims, out CommandResult? victimError))
        {
            return victimError!;
        }

        if (!TryGetSinglePlayer(state, executor, destinationToken, out Player destination, out CommandResult? destinationError))
        {
            return destinationError!;
        }

        Dimension? dimension = ResolvePlayerDestinationDimension(contextWorld, destination, explicitDimensionId);
        return TeleportPlayers(state, victims, destination.Position, dimension, destinationName: destination.Username);
    }

    static CommandResult TeleportVictimsToPosition(
        CommandExecutionState state,
        Player? executor,
        WorldInstance contextWorld,
        string? explicitDimensionId,
        string victimToken,
        string[] args,
        int positionStart)
    {
        if (!TryGetPlayers(state, executor, victimToken, out List<Player> victims, out CommandResult? victimError))
        {
            return victimError!;
        }

        Player originPlayer = victims[0];
        if (!TryParsePosition(executor, args, positionStart, originPlayer, out Vec3f position))
        {
            return CommandResult.Message("§cInvalid coordinates.", false);
        }

        Dimension? dimension = ResolveCoordsDimension(contextWorld, executor ?? originPlayer, explicitDimensionId);
        return TeleportPlayers(state, victims, position, dimension, destinationName: null);
    }

    static Player? GetExecutor(CommandExecutionState state) =>
        state.Executor is PlayerExecutor executor ? executor.Player : null;

    static CommandResult? RequireExecutor(Player? executor)
    {
        if (executor is not null)
        {
            return null;
        }

        return CommandResult.Message("You must specify a target, or be a player!", false);
    }

    static bool TryGetSinglePlayer(
        CommandExecutionState state,
        Player? context,
        string token,
        out Player player,
        out CommandResult? error)
    {
        player = null!;
        error = CommandParsing.ResolveSinglePlayerTarget(
            state.Server,
            context,
            token,
            "No online players matched the target selector",
            "Multiple entities matched the target selector, please be more specific");

        if (error is not null)
        {
            return false;
        }

        List<Player> players = CommandParsing.ResolvePlayers(state.Server, context, token);
        if (players.Count == 0)
        {
            error = CommandResult.Message("The target selector must be a player!", false);
            return false;
        }

        player = players[0];
        return true;
    }

    static bool TryGetPlayers(
        CommandExecutionState state,
        Player? context,
        string token,
        out List<Player> players,
        out CommandResult? error)
    {
        players = CommandParsing.ResolvePlayers(state.Server, context, token);
        if (players.Count == 0)
        {
            error = CommandResult.Message("No online players matched the target selector", false);
            return false;
        }

        error = null;
        return true;
    }

    static Dimension? ResolvePlayerDestinationDimension(
        WorldInstance contextWorld,
        Player destination,
        string? explicitDimensionId)
    {
        if (!string.IsNullOrWhiteSpace(explicitDimensionId))
        {
            return contextWorld.GetDimension(explicitDimensionId);
        }

        return destination.Dimension;
    }

    static Dimension? ResolveCoordsDimension(
        WorldInstance contextWorld,
        Player contextPlayer,
        string? explicitDimensionId)
    {
        if (!string.IsNullOrWhiteSpace(explicitDimensionId))
        {
            return contextWorld.GetDimension(explicitDimensionId);
        }

        return contextPlayer.Dimension ?? contextWorld.GetDimension(DimensionType.Overworld);
    }

    static bool TryParsePosition(Player? executor, string[] args, int start, Player? originPlayer, out Vec3f position)
    {
        Vec3f origin = originPlayer?.Position ?? executor?.Position ?? new Vec3f();
        return CommandParsing.TryParsePosition(args, start, origin, out position);
    }

    static CommandResult TeleportPlayers(
        CommandExecutionState state,
        List<Player> players,
        Vec3f position,
        Dimension? dimension,
        string? destinationName)
    {
        Player? executor = GetExecutor(state);
        List<string> messages = [];
        int successCount = 0;

        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            try
            {
                player.Teleport(position, dimension);
                successCount++;

                if (ReferenceEquals(executor, player))
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
