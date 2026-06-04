namespace Basalt.Server.Commands.List.Operator;

using Basalt.Protocol.Enums;
using Basalt.Protocol.Types;
using Basalt.Server.Commands;
using Basalt.Server.World.Dimension;
using Player = global::Basalt.Server.Player.Player;
using WorldInstance = Basalt.Server.World.World;

public sealed class TpCommand : Basalt.Server.Commands.Command
{
    const string HelpMessage =
        "§cUsage: /tp <destination> [dimension] | /tp <x> <y> <z> [dimension] | /tp <victim> <destination> [dimension] | /tp <victim> <x> <y> <z> [dimension]";

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

    public override string? GetHelpMessage() => HelpMessage;

    public override CommandResult? ExecuteManual(CommandExecutionState state, string[] tokens, int argumentOffset)
    {
        if (argumentOffset >= tokens.Length)
        {
            return CommandResult.Message(HelpMessage, false);
        }

        string[] args = tokens[argumentOffset..];
        Player? executor = state.Executor is PlayerExecutor playerExecutor ? playerExecutor.Player : null;
        WorldInstance contextWorld = executor?.Dimension?.World ?? state.Server.GetWorld();

        CommandParsing.TryStripTrailingDimension(contextWorld, args, out args, out string? explicitDimensionId);

        if (args.Length >= 4 && CommandParsing.TryParsePosition(args, 1, new Vec3f(), out _))
        {
            return TeleportVictimsToPosition(state, executor, contextWorld, explicitDimensionId, args[0], args, positionStart: 1);
        }

        if (args.Length == 3 && TryParsePositionWithContext(executor, args, 0, null, out Vec3f selfCoords))
        {
            if (executor is null)
            {
                return CommandResult.Message("§cYou must specify a player when running this command from console.", false);
            }

            Dimension? dimension = ResolveCoordsDimension(contextWorld, executor, explicitDimensionId);
            return TeleportPlayers([executor], selfCoords, dimension, state, isSelf: true);
        }

        if (args.Length == 2)
        {
            return TeleportVictimsToPlayer(state, executor, contextWorld, explicitDimensionId, args[0], args[1]);
        }

        if (args.Length == 1)
        {
            return TeleportExecutorToPlayer(state, executor, contextWorld, explicitDimensionId, args[0]);
        }

        return CommandResult.Message(HelpMessage, false);
    }

    static CommandResult TeleportExecutorToPlayer(
        CommandExecutionState state,
        Player? executor,
        WorldInstance contextWorld,
        string? explicitDimensionId,
        string destinationToken)
    {
        if (executor is null)
        {
            return CommandResult.Message("§cYou must specify a player when running this command from console.", false);
        }

        CommandResult? targetError = CommandParsing.ResolveSinglePlayerTarget(
            state.Server,
            executor,
            destinationToken,
            "§cNo online players matched the target selector.",
            "§cMultiple entities matched the target selector, please be more specific.");
        if (targetError is not null)
        {
            return targetError;
        }

        Player destination = CommandParsing.ResolvePlayers(state.Server, executor, destinationToken)[0];
        Dimension? dimension = ResolvePlayerDestinationDimension(contextWorld, destination, explicitDimensionId);

        return TeleportPlayers([executor], destination.Position, dimension, state, isSelf: true);
    }

    static CommandResult TeleportVictimsToPlayer(
        CommandExecutionState state,
        Player? executor,
        WorldInstance contextWorld,
        string? explicitDimensionId,
        string victimToken,
        string destinationToken)
    {
        CommandResult? victimError = CommandParsing.ResolveSinglePlayerTarget(
            state.Server,
            executor,
            victimToken,
            "§cNo online players matched the victim selector.",
            "§cMultiple entities matched the victim selector, please be more specific.");
        if (victimError is not null)
        {
            return victimError;
        }

        CommandResult? destinationError = CommandParsing.ResolveSinglePlayerTarget(
            state.Server,
            executor,
            destinationToken,
            "§cNo online players matched the destination selector.",
            "§cMultiple entities matched the destination selector, please be more specific.");
        if (destinationError is not null)
        {
            return destinationError;
        }

        List<Player> victims = CommandParsing.ResolvePlayers(state.Server, executor, victimToken);
        Player destination = CommandParsing.ResolvePlayers(state.Server, executor, destinationToken)[0];
        Dimension? dimension = ResolvePlayerDestinationDimension(contextWorld, destination, explicitDimensionId);

        return TeleportPlayers(victims, destination.Position, dimension, state, isSelf: false, destinationName: destination.Username);
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
        List<Player> victims = CommandParsing.ResolvePlayers(state.Server, executor, victimToken);
        if (victims.Count == 0)
        {
            return CommandResult.Message("§cNo online players matched the victim selector.", false);
        }

        Player? originPlayer = victims[0];
        if (!TryParsePositionWithContext(executor, args, positionStart, originPlayer, out Vec3f position))
        {
            return CommandResult.Message("§cInvalid coordinates.", false);
        }

        Dimension? dimension = ResolveCoordsDimension(contextWorld, executor ?? originPlayer, explicitDimensionId);
        return TeleportPlayers(victims, position, dimension, state, isSelf: false);
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

    static bool TryParsePositionWithContext(
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
