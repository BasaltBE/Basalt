using BasaltBE.Basalt.Commands.Enums;

namespace BasaltBE.Basalt.Commands.Types;

public sealed class CommandParameter(string name, CommandEnum parser, bool optional = false) {
    public readonly string Name = name;
    public readonly CommandEnum Parser = parser;
    public readonly bool Optional = optional;
}
