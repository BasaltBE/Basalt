using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class OperatorEnum(bool comparison = false)
    : CommandEnum(comparison ? CommandParameterType.CompareOperator : CommandParameterType.Operator) {
    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value) {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token))
            return false;
        bool valid = comparison
            ? token is "=" or "==" or "!=" or "<" or "<=" or ">" or ">="
            : token is "=" or "+=" or "-=" or "*=" or "/=" or "%=" or "<" or ">" or "><";
        if (valid) value = token.ToString();
        return valid;
    }
}
