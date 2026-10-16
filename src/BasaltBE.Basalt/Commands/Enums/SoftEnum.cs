namespace BasaltBE.Basalt.Commands.Enums;

public sealed class SoftEnum(string identifier, IEnumerable<string> options) : CustomEnum(identifier, options, soft: true);
