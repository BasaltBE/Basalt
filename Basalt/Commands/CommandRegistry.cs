namespace Basalt.Server.Commands;

using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Server;
using Basalt.Server.Commands.List.Operator;
using Basalt.Server.Item;
using EntityInstance = Basalt.Server.Entity.Entity;
using Player = global::Basalt.Server.Player.Player;
using ServerInstance = global::Basalt.Server.Server;
using ProtocolCommand = Basalt.Protocol.Types.Command;
using ProtocolCommandEnum = Basalt.Protocol.Types.CommandEnum;
using ProtocolCommandOverload = Basalt.Protocol.Types.CommandOverload;
using ProtocolCommandParameter = Basalt.Protocol.Types.CommandParameter;


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

        throw new KeyNotFoundException($"Command '{name}' was not found.");
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
        Command command = Get(commandName);
        Command target = command;
        CommandOverload overload = command.Overload;
        int argumentOffset = 1;

        if (command.Permissions.Count > 0 && executor is PlayerExecutor playerExecutor)
        {
            bool allowed = false;
            for (int i = 0; i < command.Permissions.Count; i++)
            {
                if (!playerExecutor.Player.HasPermission(command.Permissions[i]))
                {
                    continue;
                }

                allowed = true;
                break;
            }

            if (!allowed)
            {
                return CommandResult.Message("§cYou do not have permission to run this command.", false);
            }
        }

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

        CommandExecutionState state = new()
        {
            Command = commandLine,
            Executor = executor,
            Server = server,
            Overload = overload
        };

        for (int i = 0; i < overload.Parameters.Count; i++)
        {
            CommandParameter parameter = overload.Parameters[i];
            int tokenIndex = argumentOffset + i;
            if (tokenIndex >= tokens.Length)
            {
                if (parameter.Required)
                {
                    return CommandResult.Empty(false);
                }

                continue;
            }

            state.Arguments.Add(new CommandArgument(parameter.Name, ParseArgument(server, player, parameter, tokens[tokenIndex])));
        }

        return target.Execute(state);
    }

    static CommandEnum ParseArgument(ServerInstance server, Player? player, CommandParameter parameter, string token)
    {
        if (parameter.Enum == typeof(IntEnum))
        {
            return new IntEnum(int.Parse(token));
        }

        if (parameter.Enum == typeof(StringEnum))
        {
            return new StringEnum(token);
        }

        if (parameter.Enum == typeof(JsonEnum))
        {
            return new JsonEnum(token);
        }

        if (parameter.Enum == typeof(TargetEnum))
        {
            EntityInstance[] entities = ResolveTargets(server, player, token);
            string[] offlineUsernames = ResolveOfflineTargets(server, token, entities);
            return new TargetEnum(token, entities, offlineUsernames);
        }

        if (parameter.Enum == typeof(ItemEnum))
        {
            string identifier = token.IndexOf(':') == -1 ? "minecraft:" + token : token;
            ItemType type = ItemType.Get(identifier) ?? throw new InvalidOperationException($"Invalid item '{token}' for command parameter '{parameter.Name}'.");
            return new ItemEnum(token, type);
        }

        if (typeof(CustomEnum).IsAssignableFrom(parameter.Enum))
        {
            if (Activator.CreateInstance(parameter.Enum) is not CustomEnum customEnum)
            {
                throw new InvalidOperationException($"Command enum '{parameter.Enum.FullName}' could not be created.");
            }

            string? value = customEnum.Options.FirstOrDefault(option => string.Equals(option, token, StringComparison.OrdinalIgnoreCase));
            if (value is null)
            {
                throw new InvalidOperationException($"Invalid value '{token}' for command parameter '{parameter.Name}'.");
            }

            customEnum.Value = value;
            return customEnum;
        }

        throw new InvalidOperationException($"Unsupported command parameter enum: {parameter.Enum.FullName}.");
    }

    static EntityInstance[] ResolveTargets(ServerInstance server, Player? player, string token)
    {
        if (token == "@s")
        {
            return player is null ? [] : [player];
        }

        if (token == "@a")
        {
            return server.Players.Values.ToArray<EntityInstance>();
        }

        if (token == "@e")
        {
            if (player is not null)
            {
                return player.Dimension?.Entities.ToArray() ?? [];
            }

            return server.Worlds.SelectMany(world => world.Dimensions).SelectMany(dimension => dimension.Entities).ToArray();
        }

        if (token == "@p")
        {
            Player? nearest = null;
            float nearestDistance = float.MaxValue;
            foreach (Player candidate in server.Players.Values)
            {
                if (player is not null && candidate.Dimension != player.Dimension)
                {
                    continue;
                }

                float dx = candidate.Position.X - (player?.Position.X ?? candidate.Position.X);
                float dy = candidate.Position.Y - (player?.Position.Y ?? candidate.Position.Y);
                float dz = candidate.Position.Z - (player?.Position.Z ?? candidate.Position.Z);
                float distance = dx * dx + dy * dy + dz * dz;
                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistance = distance;
            }

            return nearest is null ? [] : [nearest];
        }

        foreach (Player candidate in server.Players.Values)
        {
            if (string.Equals(candidate.Username, token, StringComparison.OrdinalIgnoreCase))
            {
                return [candidate];
            }
        }

        return [];
    }

    static string[] ResolveOfflineTargets(ServerInstance server, string token, EntityInstance[] onlineTargets)
    {
        if (onlineTargets.Length > 0 || token.StartsWith('@'))
        {
            return [];
        }



        return [];
    }

    public void CacheAvailableCommands()
    {
        AvailableCommandsPacket = BuildAvailableCommandsPacket();
    }

    public AvailableCommandsPacket BuildAvailableCommandsPacket()
    {
        AvailableCommandsPacket packet = new();
        Dictionary<string, uint> enumValueOffsets = new(StringComparer.Ordinal);
        Dictionary<Type, uint> enumOffsets = new();

        foreach (Command command in Commands)
        {
            packet.Commands.Add(new ProtocolCommand
            {
                Name = command.Name,
                Description = command.Description,
                PermissionLevel = CommandPermissionLevel.Any,
                AliasesOffset = GetAliasesOffset(packet, enumValueOffsets, command),
                Overloads = BuildOverloads(packet, enumValueOffsets, enumOffsets, command)
            });
        }

        return packet;
    }

    static uint GetAliasesOffset(AvailableCommandsPacket packet, Dictionary<string, uint> enumValueOffsets, Command command)
    {
        if (command.Aliases.Count == 0)
        {
            return uint.MaxValue;
        }

        return AddEnum(packet, enumValueOffsets, command.Name + "_aliases", command.Aliases);
    }

    static List<ProtocolCommandOverload> BuildOverloads(
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        Dictionary<Type, uint> enumOffsets,
        Command command)
    {
        List<ProtocolCommandOverload> overloads = new();

        for (int i = 0; i < command.SubCommands.Count; i++)
        {
            SubCommand subCommand = command.SubCommands[i];
            List<ProtocolCommandParameter> parameters = new()
            {
                CreateEnumParameter(packet, enumValueOffsets, subCommand.Name, subCommand.Name, [subCommand.Name], required: true)
            };
            parameters.AddRange(BuildParameters(packet, enumValueOffsets, enumOffsets, subCommand.Overload));
            overloads.Add(new ProtocolCommandOverload { Parameters = parameters });
        }

        if (command.Overload.Parameters.Count > 0 || overloads.Count == 0)
        {
            overloads.Add(new ProtocolCommandOverload
            {
                Parameters = BuildParameters(packet, enumValueOffsets, enumOffsets, command.Overload)
            });
        }

        return overloads;
    }

    static List<ProtocolCommandParameter> BuildParameters(
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        Dictionary<Type, uint> enumOffsets,
        CommandOverload overload)
    {
        List<ProtocolCommandParameter> parameters = new(overload.Parameters.Count);
        for (int i = 0; i < overload.Parameters.Count; i++)
        {
            CommandParameter parameter = overload.Parameters[i];
            parameters.Add(BuildParameter(packet, enumValueOffsets, enumOffsets, parameter));
        }

        return parameters;
    }

    static ProtocolCommandParameter BuildParameter(
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        Dictionary<Type, uint> enumOffsets,
        CommandParameter parameter)
    {
        if (parameter.Enum == typeof(ItemEnum))
        {
            uint enumOffset = GetEnumOffset(packet, enumValueOffsets, enumOffsets, parameter.Enum);
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

        if (type == typeof(JsonEnum))
        {
            return CommandParameterType.Json;
        }

        throw new InvalidOperationException($"Unsupported command parameter enum: {type.FullName}.");
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
        Type type)
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








