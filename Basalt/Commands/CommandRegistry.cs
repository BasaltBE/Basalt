namespace Basalt.Core.Commands;

using System.Diagnostics.CodeAnalysis;
using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Player = Player.Player;
using ServerInstance = Server;
using ProtocolCommand = Protocol.Types.Command;
using ProtocolCommandEnum = Protocol.Types.CommandEnum;
using ProtocolCommandOverload = Protocol.Types.CommandOverload;
using ProtocolCommandParameter = Protocol.Types.CommandParameter;

public sealed class CommandRegistry
{
    readonly Dictionary<string, CommandDefinition> _commands = new(StringComparer.OrdinalIgnoreCase);
    readonly List<CommandDefinition> _definitions = [];

    public IEnumerable<CommandDefinition> Definitions => _definitions;

    public void Register(CommandDefinition definition)
    {
        _definitions.Add(definition);
        _commands[definition.Name] = definition;
        foreach (string alias in definition.Aliases)
        {
            _commands[alias] = definition;
        }
    }

    /// <summary>
    /// Finds a command definition by name or alias. Returns null if not found.
    /// </summary>
    public CommandDefinition? FindCommand(string name)
    {
        string trimmed = name.TrimStart('/');
        _commands.TryGetValue(trimmed, out CommandDefinition? def);
        return def;
    }

    public CommandResult Execute(ServerInstance server, Player player, string commandLine)
    {
        return Execute(server, new CommandSender.PlayerSender(player), commandLine);
    }

    public CommandResult Execute(ServerInstance server, string commandLine)
    {
        return Execute(server, new CommandSender.ServerSender(), commandLine);
    }

    CommandResult Execute(ServerInstance server, CommandSender sender, string commandLine)
    {
        string input = commandLine;
        if (input.Length > 0 && input[0] == '/')
            input = input[1..];

        string[] tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
            return CommandResult.Fail;

        string name = tokens[0];
        if (!_commands.TryGetValue(name, out CommandDefinition? definition))
            return CommandResult.Error($"§cUnknown command: {name}. Please check that the command exists and that you have permission to use it.");

        if (!HasPermission(sender, definition))
            return CommandResult.Error("§cYou do not have permission to run this command.");

        CommandContext ctx = new()
        {
            Server = server,
            Sender = sender,
            Raw = tokens.Length > 1 ? input[(name.Length + 1)..] : ""
        };

        string[] rawArgs = tokens.Length > 1 ? tokens[1..] : [];
        List<CommandArgument>? matched = MatchOverloads(ctx, definition, rawArgs);
        if (matched is not null)
        {
            ctx.Arguments.AddRange(matched);
        }

        return definition.Handler.Execute(ctx);
    }

    static List<CommandArgument>? MatchOverloads(CommandContext ctx, CommandDefinition definition, string[] rawArgs)
    {
        foreach (OverloadDefinition overload in definition.Overloads)
        {
            List<CommandArgument>? result = TryParseOverload(ctx, overload, rawArgs);
            if (result is not null)
                return result;
        }
        return null;
    }

    static List<CommandArgument>? TryParseOverload(CommandContext ctx, OverloadDefinition overload, string[] rawArgs)
    {
        List<CommandArgument> arguments = [];
        int tokenIdx = 0;

        foreach (ParameterDefinition param in overload.Parameters)
        {
            if (tokenIdx >= rawArgs.Length)
            {
                if (param.Optional)
                    continue;
                return null;
            }

            CommandEnum? parsed = CreateAndParse(ctx, param.Type, rawArgs, ref tokenIdx);
            if (parsed is null)
            {
                if (param.Optional)
                    continue;
                return null;
            }

            arguments.Add(new CommandArgument(param.Name, parsed));
        }

        if (tokenIdx < rawArgs.Length)
            return null;

        return arguments;
    }

    static CommandEnum? CreateAndParse(
        CommandContext ctx,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type enumType,
        string[] tokens,
        ref int tokenIndex)
    {
        if (Activator.CreateInstance(enumType) is not CommandEnum instance)
            return null;

        int saved = tokenIndex;
        if (!instance.Parse(ctx, tokens, ref tokenIndex))
        {
            tokenIndex = saved;
            return null;
        }

        return instance;
    }

    static bool HasPermission(CommandSender sender, CommandDefinition definition)
    {
        if (sender is CommandSender.ServerSender)
            return true;

        if (definition.Permissions.Length == 0)
            return true;

        if (sender is CommandSender.PlayerSender ps)
        {
            foreach (string perm in definition.Permissions)
            {
                if (ps.Player.HasPermission(perm))
                    return true;
            }
        }

        return false;
    }

    public AvailableCommandsPacket BuildAvailableCommandsPacket(Player? player = null)
    {
        AvailableCommandsPacket packet = new();
        Dictionary<string, uint> enumValueOffsets = new(StringComparer.Ordinal);
        Dictionary<Type, uint> enumOffsets = new();

        foreach (CommandDefinition def in _definitions)
        {
            if (player is not null && !HasPermission(new CommandSender.PlayerSender(player), def))
                continue;

            packet.Commands.Add(new ProtocolCommand
            {
                Name = def.Name,
                Description = def.Description,
                PermissionLevel = def.Permissions.Length == 0 ? CommandPermissionLevel.Any : CommandPermissionLevel.Admin,
                AliasesOffset = GetAliasesOffset(packet, enumValueOffsets, def),
                Overloads = BuildOverloads(packet, enumValueOffsets, enumOffsets, def)
            });
        }

        return packet;
    }

    public void SendAvailableCommands(ServerInstance server, Player player)
    {
        if (player.Connection is null)
            return;

        server.Network.SendPacket(player.Connection, BuildAvailableCommandsPacket(player));
    }

    static List<ProtocolCommandOverload> BuildOverloads(
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        Dictionary<Type, uint> enumOffsets,
        CommandDefinition def)
    {
        List<ProtocolCommandOverload> overloads = [];

        foreach (OverloadDefinition overload in def.Overloads)
        {
            List<ProtocolCommandParameter> parameters = [];
            foreach (ParameterDefinition param in overload.Parameters)
            {
                parameters.Add(BuildParameter(packet, enumValueOffsets, enumOffsets, param));
            }
            overloads.Add(new ProtocolCommandOverload { Parameters = parameters });
        }

        return overloads;
    }

    static ProtocolCommandParameter BuildParameter(
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        Dictionary<Type, uint> enumOffsets,
        ParameterDefinition param)
    {
        Type type = param.Type;

        if (type == typeof(ItemEnum) || type == typeof(EntityEnum) || type == typeof(EnchantmentEnum))
        {
            uint enumOffset = GetEnumOffset(packet, enumValueOffsets, enumOffsets, type);
            return new ProtocolCommandParameter
            {
                Name = param.Name,
                Type = (uint)CommandParameterTypeFlag.Valid | (uint)CommandParameterTypeFlag.Enum | enumOffset,
                Optional = param.Optional
            };
        }

        if (typeof(CustomEnum).IsAssignableFrom(type))
        {
            uint enumOffset = GetEnumOffset(packet, enumValueOffsets, enumOffsets, type);
            return new ProtocolCommandParameter
            {
                Name = param.Name,
                Type = (uint)CommandParameterTypeFlag.Valid | (uint)CommandParameterTypeFlag.Enum | enumOffset,
                Optional = param.Optional
            };
        }

        return new ProtocolCommandParameter
        {
            Name = param.Name,
            Type = (uint)CommandParameterTypeFlag.Valid | (uint)GetParameterType(type),
            Optional = param.Optional
        };
    }

    static CommandParameterType GetParameterType(Type type)
    {
        if (type == typeof(IntEnum)) return CommandParameterType.Int;
        if (type == typeof(TargetEnum)) return CommandParameterType.Target;
        if (type == typeof(StringEnum)) return CommandParameterType.String;
        if (type == typeof(PositionEnum)) return CommandParameterType.Position;
        if (type == typeof(JsonEnum)) return CommandParameterType.Json;
        throw new InvalidOperationException($"Unsupported command enum type: {type.FullName}.");
    }

    static uint GetAliasesOffset(AvailableCommandsPacket packet, Dictionary<string, uint> enumValueOffsets, CommandDefinition def)
    {
        if (def.Aliases.Length == 0)
            return uint.MaxValue;

        return AddEnum(packet, enumValueOffsets, def.Name + "_aliases", [def.Name, .. def.Aliases]);
    }

    static uint GetEnumOffset(
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        Dictionary<Type, uint> enumOffsets,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
    {
        if (enumOffsets.TryGetValue(type, out uint offset))
            return offset;

        if (Activator.CreateInstance(type) is not CommandEnum instance)
            throw new InvalidOperationException($"Could not create enum instance for '{type.FullName}'.");

        offset = AddEnum(packet, enumValueOffsets, instance.Identifier, instance.Options);
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
        uint offset = (uint)packet.Enums.Count;
        packet.Enums.Add(commandEnum);
        return offset;
    }

    static uint GetEnumValueOffset(
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        string value)
    {
        if (enumValueOffsets.TryGetValue(value, out uint offset))
            return offset;

        offset = (uint)packet.EnumValues.Count;
        enumValueOffsets[value] = offset;
        packet.EnumValues.Add(value);
        return offset;
    }
}
