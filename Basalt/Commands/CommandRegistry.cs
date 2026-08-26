namespace Basalt.Core.Commands;

using System.Diagnostics.CodeAnalysis;
using Basalt.BedrockProtocol.Packets;
using Basalt.BedrockProtocol.Types;
using Basalt.BedrockProtocol.Enums;
using Player = Player.Player;
using ServerInstance = Server;
using Basalt.Core.Enums;
using Basalt.Core.Plugins;

public sealed class CommandRegistry {
    readonly Dictionary<string, CommandDefinition> _commands = new(StringComparer.OrdinalIgnoreCase);
    readonly List<CommandDefinition> _definitions = [];
    internal Func<PluginContainer?>? PluginOwnerProvider { get; set; }
    internal Func<PluginContainer?, IDisposable>? PluginScopeProvider { get; set; }
    internal Action<PluginContainer?, string, Exception>? PluginErrorHandler { get; set; }

    public IEnumerable<CommandDefinition> Definitions => _definitions;

    public void Register(CommandDefinition definition) {
        definition.Owner = PluginOwnerProvider?.Invoke();
        _definitions.Add(definition);
        _commands[definition.Name] = definition;
        foreach (string alias in definition.Aliases) {
            _commands[alias] = definition;
        }
    }

    public bool Unregister(string name) {
        if (!_commands.TryGetValue(name.TrimStart('/'), out CommandDefinition? definition)) {
            return false;
        }

        _definitions.Remove(definition);
        foreach (string key in _commands.Where(pair => ReferenceEquals(pair.Value, definition)).Select(pair => pair.Key).ToArray()) {
            _commands.Remove(key);
        }

        return true;
    }

    /// <summary>
    /// Finds a command definition by name or alias. Returns null if not found.
    /// </summary>
    public CommandDefinition? FindCommand(string name) {
        string trimmed = name.TrimStart('/');
        _commands.TryGetValue(trimmed, out CommandDefinition? def);
        return def;
    }

    public CommandResult Execute(ServerInstance server, Player player, string commandLine) {
        return Execute(server, new CommandSender.PlayerSender(player), commandLine);
    }

    public CommandResult Execute(ServerInstance server, string commandLine) {
        return Execute(server, new CommandSender.ServerSender(), commandLine);
    }

    CommandResult Execute(ServerInstance server, CommandSender sender, string commandLine) {
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

        CommandContext ctx = new() {
            Server = server,
            Sender = sender,
            Raw = tokens.Length > 1 ? input[(name.Length + 1)..] : ""
        };

        string[] rawArgs = tokens.Length > 1 ? tokens[1..] : [];
        List<CommandArgument>? matched = MatchOverloads(ctx, definition, rawArgs);
        if (matched is not null) {
            ctx.Arguments.AddRange(matched);
        }

        try {
            using (PluginScopeProvider?.Invoke(definition.Owner) ?? EmptyScope.Instance)
                return definition.Handler.Execute(ctx);
        }
        catch (Exception exception) {
            PluginErrorHandler?.Invoke(definition.Owner, $"command /{definition.Name}", exception);
            return CommandResult.Error("§cThe command failed while executing.");
        }
    }

    internal void RemovePluginCommands(PluginContainer plugin) {
        foreach (CommandDefinition definition in _definitions
            .Where(definition => ReferenceEquals(definition.Owner, plugin))
            .ToArray()) {
            Unregister(definition.Name);
        }
    }

    private sealed class EmptyScope : IDisposable {
        public static EmptyScope Instance { get; } = new();

        public void Dispose() {
        }
    }

    static List<CommandArgument>? MatchOverloads(CommandContext ctx, CommandDefinition definition, string[] rawArgs) {
        foreach (OverloadDefinition overload in definition.Overloads) {
            List<CommandArgument>? result = TryParseOverload(ctx, overload, rawArgs);
            if (result is not null)
                return result;
        }
        return null;
    }

    static List<CommandArgument>? TryParseOverload(CommandContext ctx, OverloadDefinition overload, string[] rawArgs) {
        List<CommandArgument> arguments = [];
        int tokenIdx = 0;

        foreach (ParameterDefinition param in overload.Parameters) {
            if (tokenIdx >= rawArgs.Length) {
                if (param.Optional)
                    continue;
                return null;
            }

            CommandEnum? parsed = CreateAndParse(ctx, param.Type, rawArgs, ref tokenIdx);
            if (parsed is null) {
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
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] System.Type enumType,
        string[] tokens,
        ref int tokenIndex) {
        if (Activator.CreateInstance(enumType) is not CommandEnum instance)
            return null;

        int saved = tokenIndex;
        if (!instance.Parse(ctx, tokens, ref tokenIndex)) {
            tokenIndex = saved;
            return null;
        }

        return instance;
    }

    static bool HasPermission(CommandSender sender, CommandDefinition definition) {
        if (sender is CommandSender.ServerSender)
            return true;

        if (definition.Permissions.Length == 0)
            return true;

        if (sender is CommandSender.PlayerSender ps) {
            foreach (string perm in definition.Permissions) {
                if (ps.Player.HasPermission(perm))
                    return true;
            }
        }

        return false;
    }

    public AvailableCommandsPacket BuildAvailableCommandsPacket(Player? player = null) {
        AvailableCommandsPacket packet = new() {
            EnumValues = [],
            ChainedSubcommandValues = [],
            PostFixes = [],
            Enums = [],
            ChainedSubcommands = [],
            Commands = [],
            SoftEnums = [],
            Constraints = []
        };

        Dictionary<string, uint> enumValueOffsets = new(StringComparer.Ordinal);
        Dictionary<System.Type, int> enumOffsets = new();

        foreach (CommandDefinition def in _definitions) {
            if (
                player is not null &&
                !HasPermission(new CommandSender.PlayerSender(player), def)
            ) {
                continue;
            }

            packet.Commands = [.. packet.Commands, new AvailableCommandsCommandData {
                Name = def.Name,
                Description = def.Description,
                Flags = 0,
                PermissionLevel = "any",
                AliasEnum = GetAliasesOffset(packet, enumValueOffsets, def),
                ChainedSubcommandIndexes = [],
                Overloads = BuildOverloads(
                    packet,
                    enumValueOffsets,
                    enumOffsets,
                    def
                ).ToArray()
            }];
        }

        return packet;
    }

    public void SendAvailableCommands(ServerInstance server, Player player) {
        if (player.Connection is null) {
            return;
        }

        AvailableCommandsPacket packet = BuildAvailableCommandsPacket(player);
        server.Network.QueuePacket(
            player.Connection,
            packet
        );
    }

    static List<AvailableCommandsOverloadData> BuildOverloads(
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        Dictionary<System.Type, int> enumOffsets,
        CommandDefinition def
    ) {
        List<AvailableCommandsOverloadData> overloads = [];

        foreach (OverloadDefinition overload in def.Overloads) {
            List<AvailableCommandsParamData> parameters = [];

            foreach (ParameterDefinition param in overload.Parameters) {
                parameters.Add(
                    BuildParameter(
                        packet,
                        enumValueOffsets,
                        enumOffsets,
                        param
                    )
                );
            }

            overloads.Add(new AvailableCommandsOverloadData {
                IsChaining = false,
                Parameters = [.. parameters]
            });
        }

        return overloads;
    }

    static AvailableCommandsParamData BuildParameter(
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        Dictionary<System.Type, int> enumOffsets,
        ParameterDefinition param
    ) {
        System.Type type = param.Type;

        if (
            type == typeof(ItemEnum) ||
            type == typeof(EntityEnum) ||
            type == typeof(EnchantmentEnum) ||
            type == typeof(BlockEnum) ||
            typeof(CustomEnum).IsAssignableFrom(type)
        ) {
            int enumOffset = GetEnumOffset(
                packet,
                enumValueOffsets,
                enumOffsets,
                type
            );

            return new AvailableCommandsParamData {
                Name = param.Name,
                ParseSymbol =
                    (uint)CommandParameterTypeFlag.Valid |
                    (uint)CommandParameterTypeFlag.Enum |
                    unchecked((uint)enumOffset),
                Optional = param.Optional,
                Options = 0
            };
        }

        return new AvailableCommandsParamData {
            Name = param.Name,
            ParseSymbol =
                (uint)CommandParameterTypeFlag.Valid |
                (uint)GetParameterType(type),
            Optional = param.Optional,
            Options = 0
        };
    }

    static CommandParameterType GetParameterType(System.Type type) {
        if (typeof(IntEnum).IsAssignableFrom(type))
            return CommandParameterType.Int;

        if (type == typeof(TargetEnum))
            return CommandParameterType.Target;

        if (type == typeof(StringEnum))
            return CommandParameterType.String;

        if (type == typeof(PositionEnum))
            return CommandParameterType.Position;

        if (type == typeof(JsonEnum))
            return CommandParameterType.Json;

        throw new InvalidOperationException(
            $"Unsupported command enum type: {type.FullName}."
        );
    }

    static int GetAliasesOffset(
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        CommandDefinition def
    ) {
        if (def.Aliases.Length == 0) {
            return -1;
        }

        return AddEnum(
            packet,
            enumValueOffsets,
            def.Name + "_aliases",
            [def.Name, .. def.Aliases]
        );
    }

    static int GetEnumOffset(
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        Dictionary<System.Type, int> enumOffsets,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
        )]
        System.Type type
    ) {
        if (enumOffsets.TryGetValue(type, out int offset)) {
            return offset;
        }

        if (Activator.CreateInstance(type) is not CommandEnum instance) {
            throw new InvalidOperationException(
                $"Could not create enum instance for '{type.FullName}'."
            );
        }

        offset = AddEnum(
            packet,
            enumValueOffsets,
            instance.Identifier,
            instance.Options
        );

        enumOffsets[type] = offset;
        return offset;
    }

    static int AddEnum(
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        string name,
        IEnumerable<string> values
    ) {
        List<uint> valueIndices = [];

        foreach (string value in values) {
            valueIndices.Add(
                GetEnumValueOffset(
                    packet,
                    enumValueOffsets,
                    value
                )
            );
        }

        int offset = packet.Enums.Length;

        packet.Enums = [.. packet.Enums, new AvailableCommandsEnumData {
            Name = name,
            Values = valueIndices.ToArray()
        }];

        return offset;
    }

    static uint GetEnumValueOffset(
        AvailableCommandsPacket packet,
        Dictionary<string, uint> enumValueOffsets,
        string value
    ) {
        if (enumValueOffsets.TryGetValue(value, out uint offset)) {
            return offset;
        }

        offset = checked((uint)packet.EnumValues.Length);
        enumValueOffsets[value] = offset;
        packet.EnumValues = [.. packet.EnumValues, value];
        return offset;
    }

}
