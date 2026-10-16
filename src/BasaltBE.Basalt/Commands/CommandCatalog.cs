using Basalt.Binary;
using BasaltBE.Basalt.Commands.Enums;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using BinaryWriter = Basalt.Binary.BinaryWriter;

namespace BasaltBE.Basalt.Commands;

internal sealed class CommandCatalog
{
    public readonly AvailableCommandsPacket Packet;
    public readonly byte[] Payload;

    public CommandCatalog(IEnumerable<Command> definitions)
    {
        List<string> values = [];
        Dictionary<string, uint> valueIndexes = new(StringComparer.Ordinal);
        List<AvailableCommandsEnumData> enums = [];
        HashSet<string> enumNames = new(StringComparer.Ordinal);
        List<AvailableCommandsSoftEnumData> softEnums = [];
        Dictionary<CommandEnum, uint> symbols = new(ReferenceEqualityComparer.Instance);
        Dictionary<string, CommandEnum> names = new(StringComparer.Ordinal);
        List<AvailableCommandsCommandData> commands = [];

        foreach (Command definition in definitions)
        {
            int aliasIndex = -1;
            if (definition.Aliases.Length > 0)
            {
                aliasIndex = enums.Count;
                AddEnum(definition.Name + "_aliases", [definition.Name, .. definition.Aliases]);
            }
            AvailableCommandsOverloadData[] overloads = new AvailableCommandsOverloadData[definition.Overloads.Count];
            for (int index = 0; index < overloads.Length; index++)
            {
                var parameters = definition.Overloads[index].Parameters;
                AvailableCommandsParamData[] encoded = new AvailableCommandsParamData[parameters.Length];
                for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
                {
                    var parameter = parameters[parameterIndex];
                    encoded[parameterIndex] = new AvailableCommandsParamData
                    {
                        Name = parameter.Name,
                        ParseSymbol = GetParseSymbol(parameter.Parser),
                        Optional = parameter.Optional
                    };
                }
                overloads[index] = new AvailableCommandsOverloadData { Parameters = encoded };
            }
            commands.Add(new AvailableCommandsCommandData
            {
                Name = definition.Name,
                Description = definition.Description,
                PermissionLevel = "any",
                AliasEnum = aliasIndex,
                Overloads = overloads
            });
        }
        Packet = new AvailableCommandsPacket
        {
            EnumValues = values.ToArray(),
            Enums = enums.ToArray(),
            SoftEnums = softEnums.ToArray(),
            Commands = commands.ToArray()
        };
        using BinaryStream stream = BinaryStream.Rent(1024 * 1024);
        BinaryWriter writer = stream.GetWriter();
        Packet.Serialize(ref writer);
        Payload = stream.Buffer.Span[..stream.Offset].ToArray();

        uint GetParseSymbol(CommandEnum parser)
        {
            if (symbols.TryGetValue(parser, out uint symbol))
                return symbol;
            if (parser is not CustomEnum custom)
            {
                symbol = (uint)CommandParameterTypeFlag.Valid | (uint)parser.Type;
                symbols.Add(parser, symbol);
                return symbol;
            }
            if (names.TryGetValue(custom.Identifier, out CommandEnum? existing))
            {
                if (existing is not CustomEnum previous || previous.Soft != custom.Soft ||
                    !previous.Options.Order(StringComparer.Ordinal).SequenceEqual(custom.Options.Order(StringComparer.Ordinal)))
                    throw new InvalidOperationException($"Conflicting command enum '{custom.Identifier}'.");
                symbol = symbols[existing];
            }
            else
            {
                names.Add(custom.Identifier, parser);
                if (custom.Soft)
                {
                    symbol = (uint)CommandParameterTypeFlag.Valid | (uint)CommandParameterTypeFlag.SoftEnum | (uint)softEnums.Count;
                    softEnums.Add(new AvailableCommandsSoftEnumData { Name = custom.Identifier, Values = custom.Options.ToArray() });
                }
                else
                {
                    symbol = (uint)CommandParameterTypeFlag.Valid | (uint)CommandParameterTypeFlag.Enum | (uint)enums.Count;
                    AddEnum(custom.Identifier, custom.Options);
                }
            }
            symbols.Add(parser, symbol);
            return symbol;
        }

        void AddEnum(string name, IEnumerable<string> options)
        {
            if (!enumNames.Add(name))
                throw new InvalidOperationException($"Duplicate command enum name '{name}'.");
            List<uint> indexes = [];
            foreach (string option in options)
            {
                if (!valueIndexes.TryGetValue(option, out uint offset))
                {
                    offset = (uint)values.Count;
                    valueIndexes.Add(option, offset);
                    values.Add(option);
                }
                indexes.Add(offset);
            }
            enums.Add(new AvailableCommandsEnumData { Name = name, Values = indexes.ToArray() });
        }
    }
}
