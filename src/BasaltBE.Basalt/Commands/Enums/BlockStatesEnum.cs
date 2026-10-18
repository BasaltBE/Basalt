using System.Globalization;
using BasaltBE.Basalt.Blocks.Types;
using BasaltBE.Protocol.Enums;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class BlockStatesEnum(bool array = false)
    : CommandEnum(array ? CommandParameterType.BlockStateArray : CommandParameterType.BlockStates) {
    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value) {
        value = null;
        if (!reader.Read(out ReadOnlySpan<char> token) || token.Length < 2 || token[0] != '[' || token[^1] != ']')
            return false;
        BlockState states = new();
        ReadOnlySpan<char> remaining = token[1..^1].Trim();
        while (!remaining.IsEmpty) {
            int end = 0;
            bool quoted = false;
            bool escaped = false;
            for (; end < remaining.Length; end++) {
                char character = remaining[end];
                if (escaped) escaped = false;
                else if (quoted && character == '\\') escaped = true;
                else if (character == '"') quoted = !quoted;
                else if (!quoted && character == ',') break;
            }
            ReadOnlySpan<char> state = remaining[..end].Trim();
            int equals = state.IndexOf('=');
            if (equals <= 0 || equals == state.Length - 1 || states.Values.Count == 32)
                return false;
            ReadOnlySpan<char> key = state[..equals].Trim();
            ReadOnlySpan<char> text = state[(equals + 1)..].Trim();
            if (key.Length < 2 || key[0] != '"' || key[^1] != '"' || text.IsEmpty)
                return false;
            object parsed;
            if (text.Length >= 2 && text[0] == '"' && text[^1] == '"') parsed = CommandReader.Text(text);
            else if (text.SequenceEqual("true")) parsed = true;
            else if (text.SequenceEqual("false")) parsed = false;
            else if (int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int integer)) parsed = integer;
            else return false;
            if (!states.Values.TryAdd(CommandReader.Text(key), parsed))
                return false;
            if (end == remaining.Length)
                break;
            remaining = remaining[(end + 1)..].Trim();
            if (remaining.IsEmpty)
                return false;
        }
        value = states;
        return true;
    }
}
