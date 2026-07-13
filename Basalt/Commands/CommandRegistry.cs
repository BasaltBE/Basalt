namespace Basalt.Core.Commands;

using System.Diagnostics.CodeAnalysis;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Core;
using Basalt.Core.Commands.List.Operator;
using Player = Player.Player;
using ServerInstance = Server;
using DimensionInstance = Worlds.Dimensions.Dimension;
using WorldInstance = Worlds.World;
using ProtocolCommand = Protocol.Types.Command;
using ProtocolCommandEnum = Protocol.Types.CommandEnum;
using ProtocolCommandOverload = Protocol.Types.CommandOverload;
using ProtocolCommandParameter = Protocol.Types.CommandParameter;


public class CommandRegistry
{
    private readonly Dictionary<string, Command> _commands = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<Command> Commands => _commands.Values.Distinct();

    public AvailableCommandsPacket AvailableCommandsPacket = new();

    public void RegisterDefaultCommands()
    {
        Register(new StatusCommand());
        Register(new ClearCommand());
        Register(new GamemodeCommand());
        Register(new GiveCommand());
        Register(new OpCommand());
        Register(new DeopCommand());
        Register(new ListCommand());
        Register(new SummonCommand());
        Register(new TpCommand());
        Register(new PluginsCommand());
    }

    public void Register(Command command)
    {
        _commands[command.Name] = command;
        for (int i = 0; i < command.Aliases.Count; i++)
        {
            _commands[command.Aliases[i]] = command;
        }
    }

    public void Unregister(string name)
    {
        Command command = Get(name);
        _commands.Remove(command.Name);
        for (int i = 0; i < command.Aliases.Count; i++)
        {
            _commands.Remove(command.Aliases[i]);
        }
    }

    public Command Get(string name)
    {
        if (_commands.TryGetValue(name, out Command? command))
        {
            return command;
        }

        throw new KeyNotFoundException($"Could not find command '{name}'.");
    }

    public const string PermissionDeniedMessage = "§cYou do not have permission to run this command.";

    public static bool CanPlayerExecute(Command command, Player player)
    {
        if (command.Permissions.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < command.Permissions.Count; i++)
        {
            if (player.HasPermission(command.Permissions[i]))
            {
                return true;
            }
        }

        return false;
    }

    public CommandResult Execute(ServerInstance server, Player player, string commandLine)
    {
        return Execute(server, new PlayerExecutor { Player = player }, player, commandLine);
    }

    public CommandResult Execute(ServerInstance server, string commandLine)
    {
        return Execute(server, new ServerExecutor(), null, commandLine);
    }

    CommandResult Execute(ServerInstance server, ICommandExecutor executor, Player? player, string commandLine)
    {
        string[] tokens = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            return CommandResult.Empty(false);
        }

        string commandName = tokens[0].TrimStart('/');
        if (!_commands.TryGetValue(commandName, out Command? command))
        {
            return CommandResult.Message($"§cCommand '{commandName}' was not found.", false);
        }

        Command target = command;
        CommandOverload overload = command.Overload;
        int argumentOffset = 1;

        if (tokens.Length > 1)
        {
            for (int i = 0; i < command.SubCommands.Count; i++)
            {
                SubCommand subCommand = command.SubCommands[i];
                if (!string.Equals(subCommand.Name, tokens[1], StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                target = subCommand;
                overload = subCommand.Overload;
                argumentOffset = 2;
                break;
            }
        }

        if (executor is PlayerExecutor playerExecutor &&
            (!CanPlayerExecute(command, playerExecutor.Player) || !CanPlayerExecute(target, playerExecutor.Player)))
        {
            return CommandResult.Message(PermissionDeniedMessage, false);
        }

        CommandExecutionState state = new()
        {
            Command = commandLine,
            Executor = executor,
            Server = server,
            Overload = overload
        };

        string? helpMessage = target.GetHelpMessage();
        if (argumentOffset >= tokens.Length && helpMessage is not null)
        {
            return CommandResult.Message(helpMessage, false);
        }

        CommandResult? manualResult = target.ExecuteManual(state, tokens, argumentOffset);
        if (manualResult is not null)
        {
            return manualResult;
        }

        int tokenIndex = argumentOffset;
        for (int i = 0; i < overload.Parameters.Count; i++)
        {
            CommandParameter parameter = overload.Parameters[i];
            if (tokenIndex >= tokens.Length)
            {
                if (parameter.Required)
                {
                    return helpMessage is not null
                        ? CommandResult.Message(helpMessage, false)
                        : CommandResult.Empty(false);
                }

                continue;
            }

            CommandEnum? parsed = ParseArgument(state, parameter, tokens, ref tokenIndex);
            if (parsed is null)
            {
                if (parameter.Required)
                {
                    return helpMessage is not null
                        ? CommandResult.Message(helpMessage, false)
                        : CommandResult.Empty(false);
                }

                continue;
            }

            state.Arguments.Add(new CommandArgument(parameter.Name, parsed));
        }

        return target.Execute(state);
    }

    static CommandEnum? ParseArgument(CommandExecutionState state, CommandParameter parameter, string[] tokens, ref int tokenIndex)
    {
        if (tokenIndex >= tokens.Length)
        {
            return null;
        }

        if (Activator.CreateInstance(parameter.Enum) is not CommandEnum parsed)
        {
            throw new InvalidOperationException($"Command enum '{parameter.Enum.FullName}' could not be created.");
        }

        if (!parsed.Parse(state, parameter, tokens, ref tokenIndex))
        {
            return null;
        }

        return parsed;
    }

    public void CacheAvailableCommands(ServerInstance server)
    {
        AvailableCommandsPacket = BuildAvailableCommandsPacket(server);
    }

    public void SendAvailableCommands(ServerInstance server, Player player)
    {
        if (player.Connection is null)
        {
            return;
        }

        server.Network.SendPacket(player.Connection, BuildAvailableCommandsPacket(server, player));
    }

    public AvailableCommandsPacket BuildAvailableCommandsPacket(ServerInstance server, Player? player = null)
    {
        AvailableCommandsPacket packet = new();
        Dictionary<string, uint> enumValueOffsets = new(StringComparer.Ordinal);
        Dictionary<Type, uint> enumOffsets = new();

        foreach (Command command in Commands)
        {
            if (player is not null && !CanPlayerExecute(command, player))
            {
                continue;
            }

            packet.Commands.Add(new ProtocolCommand
            {
                Name = command.Name,
                Description = command.Description,
                PermissionLevel = GetCommandPermissionLevel(command),
                AliasesOffset = GetAliasesOffset(packet, enumValueOffsets, command),
                Overloads = BuildOverloads(server, packet, enumValueOffsets, enumOffsets, command, player)
            });
        }

        return packet;
    }

    static CommandPermissionLevel GetCommandPermissionLevel(Command command)
    {
        return command.Permissions.Count == 0
            ? CommandPermissionLevel.Any
            : CommandPermissionLevel.Admin;
    }

    static uint GetAliasesOffset(AvailableCommandsPacket packet, Dictionary<string, uint> enumValueOffsets, Command command)
    {
        if (command.Aliases.Count == 0)
        {
            return uint.MaxValue;
        }

        return AddEnum(packet, enumValueOffsets, command.Name + "_aliases", command.Aliases.Prepend(command.Name));
    }

    static List<ProtocolCommandOverload> BuildOverloads(
        ServerInstance server,
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        Dictionary<Type, uint> enumOffsets,
        Command command,
        Player? player)
    {
        List<ProtocolCommandOverload> overloads = new();

        for (int i = 0; i < command.SubCommands.Count; i++)
        {
            SubCommand subCommand = command.SubCommands[i];
            if (player is not null && !CanPlayerExecute(subCommand, player))
            {
                continue;
            }

            List<ProtocolCommandParameter> parameters = new()
            {
                CreateEnumParameter(packet, enumValueOffsets, subCommand.Name, subCommand.Name, [subCommand.Name], required: true)
            };
            parameters.AddRange(BuildParameters(server, player, packet, enumValueOffsets, enumOffsets, subCommand.Overload));
            overloads.Add(new ProtocolCommandOverload { Parameters = parameters });
        }

        for (int i = 0; i < command.DisplayOverloads.Count; i++)
        {
            overloads.Add(new ProtocolCommandOverload
            {
                Parameters = BuildParameters(server, player, packet, enumValueOffsets, enumOffsets, command.DisplayOverloads[i])
            });
        }

        if (command.Overload.Parameters.Count > 0 || overloads.Count == 0)
        {
            overloads.Add(new ProtocolCommandOverload
            {
                Parameters = BuildParameters(server, player, packet, enumValueOffsets, enumOffsets, command.Overload)
            });
        }

        return overloads;
    }

    static List<ProtocolCommandParameter> BuildParameters(
        ServerInstance server,
        Player? player,
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        Dictionary<Type, uint> enumOffsets,
        CommandOverload overload)
    {
        List<ProtocolCommandParameter> parameters = new(overload.Parameters.Count);
        for (int i = 0; i < overload.Parameters.Count; i++)
        {
            CommandParameter parameter = overload.Parameters[i];
            parameters.Add(BuildParameter(server, player, packet, enumValueOffsets, enumOffsets, parameter));
        }

        return parameters;
    }

    static ProtocolCommandParameter BuildParameter(
        ServerInstance server,
        Player? player,
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        Dictionary<Type, uint> enumOffsets,
        CommandParameter parameter)
    {
        if (parameter.Enum == typeof(ItemEnum) || parameter.Enum == typeof(EntityEnum))
        {
            uint enumOffset = GetEnumOffset(packet, enumValueOffsets, enumOffsets, parameter.Enum);
            return new ProtocolCommandParameter
            {
                Name = parameter.Name,
                Type = (uint)CommandParameterTypeFlag.Valid | (uint)CommandParameterTypeFlag.Enum | enumOffset,
                Optional = !parameter.Required
            };
        }

        if (string.Equals(parameter.Name, "dimension", StringComparison.OrdinalIgnoreCase))
        {
            WorldInstance world = player?.Dimension?.World ?? server.GetWorld();
            uint enumOffset = AddEnum(packet, enumValueOffsets, "dimension", GetRegisteredDimensionIdentifiers(world));
            return new ProtocolCommandParameter
            {
                Name = parameter.Name,
                Type = (uint)CommandParameterTypeFlag.Valid | (uint)CommandParameterTypeFlag.Enum | enumOffset,
                Optional = !parameter.Required
            };
        }

        if (typeof(CustomEnum).IsAssignableFrom(parameter.Enum))
        {
            uint enumOffset = GetEnumOffset(packet, enumValueOffsets, enumOffsets, parameter.Enum);
            return new ProtocolCommandParameter
            {
                Name = parameter.Name,
                Type = (uint)CommandParameterTypeFlag.Valid | (uint)CommandParameterTypeFlag.Enum | enumOffset,
                Optional = !parameter.Required
            };
        }

        return new ProtocolCommandParameter
        {
            Name = parameter.Name,
            Type = (uint)CommandParameterTypeFlag.Valid | (uint)GetParameterType(parameter.Enum),
            Optional = !parameter.Required
        };
    }

    static CommandParameterType GetParameterType(Type type)
    {
        if (type == typeof(IntEnum))
        {
            return CommandParameterType.Int;
        }

        if (type == typeof(TargetEnum))
        {
            return CommandParameterType.Target;
        }

        if (type == typeof(StringEnum))
        {
            return CommandParameterType.String;
        }

        if (type == typeof(PositionEnum))
        {
            return CommandParameterType.Position;
        }

        if (type == typeof(JsonEnum))
        {
            return CommandParameterType.Json;
        }

        throw new InvalidOperationException($"Unsupported command parameter enum: {type.FullName}.");
    }

    static string[] GetRegisteredDimensionIdentifiers(WorldInstance world)
    {
        List<string> identifiers = [];
        foreach (DimensionInstance dimension in world.Dimensions)
        {
            identifiers.Add(dimension.Identifier);
        }

        identifiers.Sort(StringComparer.OrdinalIgnoreCase);
        return identifiers.ToArray();
    }

    static ProtocolCommandParameter CreateEnumParameter(
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        string name,
        string type,
        IEnumerable<string> values,
        bool required)
    {
        uint enumOffset = AddEnum(packet, enumValueOffsets, type, values);
        return new ProtocolCommandParameter
        {
            Name = name,
            Type = (uint)CommandParameterTypeFlag.Valid | (uint)CommandParameterTypeFlag.Enum | enumOffset,
            Optional = !required
        };
    }

    static uint GetEnumOffset(
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        Dictionary<Type, uint> enumOffsets,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
    {
        if (enumOffsets.TryGetValue(type, out uint offset))
        {
            return offset;
        }

        if (Activator.CreateInstance(type) is not CommandEnum commandEnum)
        {
            throw new InvalidOperationException($"Command enum '{type.FullName}' could not be created.");
        }

        offset = AddEnum(packet, enumValueOffsets, commandEnum.Identifier, commandEnum.Options);
        enumOffsets[type] = offset;
        return offset;
    }

    static uint AddEnum(
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        string type,
        IEnumerable<string> values)
    {
        ProtocolCommandEnum commandEnum = new() { Type = type };
        foreach (string value in values)
        {
            commandEnum.ValueIndices.Add(GetEnumValueOffset(packet, enumValueOffsets, value));
        }

        uint offset = checked((uint)packet.Enums.Count);
        packet.Enums.Add(commandEnum);
        return offset;
    }

    static uint GetEnumValueOffset(
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        string value)
    {
        if (enumValueOffsets.TryGetValue(value, out uint offset))
        {
            return offset;
        }

        offset = checked((uint)packet.EnumValues.Count);
        enumValueOffsets[value] = offset;
        packet.EnumValues.Add(value);
        return offset;
    }
}








