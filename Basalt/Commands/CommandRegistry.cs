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
        Register(new SummonCommand());
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

        if (executor is PlayerExecutor playerExecutor && !CanPlayerExecute(target, playerExecutor.Player))
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

        int tokenIndex = argumentOffset;
        for (int i = 0; i < overload.Parameters.Count; i++)
        {
            CommandParameter parameter = overload.Parameters[i];
            if (tokenIndex >= tokens.Length)
            {
                if (parameter.Required)
                {
                    return CommandResult.Empty(false);
                }

                continue;
            }

            CommandEnum? parsed = ParseArgument(server, player, parameter, tokens, ref tokenIndex);
            if (parsed is null)
            {
                if (parameter.Required)
                {
                    return CommandResult.Empty(false);
                }

                continue;
            }

            state.Arguments.Add(new CommandArgument(parameter.Name, parsed));
        }

        return target.Execute(state);
    }

    static CommandEnum? ParseArgument(ServerInstance server, Player? player, CommandParameter parameter, string[] tokens, ref int tokenIndex)
    {
        if (tokenIndex >= tokens.Length)
        {
            return null;
        }

        string token = tokens[tokenIndex];
        if (parameter.Enum == typeof(IntEnum))
        {
            tokenIndex++;
            return new IntEnum(int.Parse(token));
        }

        if (parameter.Enum == typeof(StringEnum))
        {
            tokenIndex++;
            return new StringEnum(token);
        }

        if (parameter.Enum == typeof(JsonEnum))
        {
            tokenIndex++;
            return new JsonEnum(token);
        }

        if (parameter.Enum == typeof(TargetEnum))
        {
            tokenIndex++;
            EntityInstance[] entities = ResolveTargets(server, player, token);
            string[] offlineUsernames = ResolveOfflineTargets(server, token, entities);
            return new TargetEnum(token, entities, offlineUsernames);
        }

        if (parameter.Enum == typeof(ItemEnum))
        {
            tokenIndex++;
            string identifier = token.IndexOf(':') == -1 ? "minecraft:" + token : token;
            ItemType type = ItemType.Get(identifier) ?? throw new InvalidOperationException($"Invalid item '{token}' for command parameter '{parameter.Name}'.");
            return new ItemEnum(token, type);
        }

        if (parameter.Enum == typeof(EntityEnum))
        {
            tokenIndex++;
            string identifier = token.IndexOf(':') == -1 ? "minecraft:" + token : token;
            Entity.EntityType type = Entity.EntityType.Get(identifier) ?? throw new InvalidOperationException($"Invalid entity '{token}' for command parameter '{parameter.Name}'.");
            return new EntityEnum(token, type.Identifier);
        }

        if (parameter.Enum == typeof(PositionEnum))
        {
            if (tokenIndex + 2 >= tokens.Length)
            {
                return null;
            }

            string xToken = tokens[tokenIndex];
            string yToken = tokens[tokenIndex + 1];
            string zToken = tokens[tokenIndex + 2];

            if (!TryParsePositionComponent(xToken, player?.Position.X ?? 0f, out float x) ||
                !TryParsePositionComponent(yToken, player?.Position.Y ?? 0f, out float y) ||
                !TryParsePositionComponent(zToken, player?.Position.Z ?? 0f, out float z))
            {
                return null;
            }

            tokenIndex += 3;
            return new PositionEnum(new Basalt.Protocol.Types.Vec3f { X = x, Y = y, Z = z });
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
            tokenIndex++;
            return customEnum;
        }

        throw new InvalidOperationException($"Unsupported command parameter enum: {parameter.Enum.FullName}.");
    }

    static bool TryParsePositionComponent(string token, float origin, out float value)
    {
        value = 0f;
        if (token == "~")
        {
            value = origin;
            return true;
        }

        if (token.StartsWith('~'))
        {
            string offset = token[1..];
            if (offset.Length == 0)
            {
                value = origin;
                return true;
            }

            if (!float.TryParse(offset, out float step))
            {
                return false;
            }

            value = origin + step;
            return true;
        }

        return float.TryParse(token, out value);
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

    public void SendAvailableCommands(ServerInstance server, Player player)
    {
        if (player.Connection is null)
        {
            return;
        }

        server.Network.SendPacket(player.Connection, BuildAvailableCommandsPacket(player));
    }

    public AvailableCommandsPacket BuildAvailableCommandsPacket(Player? player = null)
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
                Overloads = BuildOverloads(packet, enumValueOffsets, enumOffsets, command)
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








