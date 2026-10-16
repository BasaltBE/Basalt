using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class TargetEnum(bool playersOnly = false) : CommandEnum(CommandParameterType.Target)
{
    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value)
    {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token) || !EntitySelector.Parse(token, out EntitySelector? selector))
            return false;
        value = selector!.Resolve(context, playersOnly);
        return true;
    }
}
