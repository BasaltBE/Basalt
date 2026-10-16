using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

// I think this is used for scoreboards
public sealed class WildcardTargetEnum() : CommandEnum(CommandParameterType.WildcardTarget)
{
    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value)
    {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token) ||
            token[0] == '@' && !EntitySelector.Parse(token, out _))
            return false;
        value = CommandReader.Text(token);
        return true;
    }
}
