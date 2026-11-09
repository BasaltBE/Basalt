using System.Text.Json;

namespace BasaltBE.Basalt.Commands;

public ref struct CommandReader(ReadOnlySpan<char> input) {
    private int offset;
    public readonly ReadOnlySpan<char> Input = input;
    public readonly bool Complete => Input[offset..].Trim().IsEmpty;
    public readonly int Offset => offset;

    public bool Read(out ReadOnlySpan<char> token) {
        while (offset < Input.Length && char.IsWhiteSpace(Input[offset]))
            offset++;

        int start = offset;
        Span<char> closings = stackalloc char[16];
        int depth = 0;
        bool quoted = false;
        bool escaped = false;

        while (offset < Input.Length) {
            char character = Input[offset];
            if (quoted) {
                if (escaped) {
                    if (character == 'u') {
                        if (offset + 4 >= Input.Length || !char.IsAsciiHexDigit(Input[offset + 1]) ||
                            !char.IsAsciiHexDigit(Input[offset + 2]) || !char.IsAsciiHexDigit(Input[offset + 3]) ||
                            !char.IsAsciiHexDigit(Input[offset + 4])) {
                            token = default;
                            return false;
                        }

                        offset += 4;
                    } else if (character is not ('"' or '\\' or '/' or 'b' or 'f' or 'n' or 'r' or 't')) {
                        token = default;
                        return false;
                    }
                    escaped = false;
                } else if (character == '\\')
                    escaped = true;
                else if (character == '"') {
                    quoted = false;

                    if (depth == 0 && offset + 1 < Input.Length && !char.IsWhiteSpace(Input[offset + 1])) {
                        token = default;
                        return false;
                    }
                }
            } else if (character == '"') {
                if (depth == 0 && offset != start) {
                    token = default;
                    return false;
                }

                quoted = true;
            } else if (character is '[' or '{') {
                if (depth == closings.Length) break;
                closings[depth++] = character == '[' ? ']' : '}';
            } else if (character is ']' or '}') {
                if (depth == 0 || closings[--depth] != character) break;
            } else if (depth == 0 && char.IsWhiteSpace(character))
                break;

            offset++;
        }

        token = Input[start..offset];
        return !token.IsEmpty && depth == 0 && !quoted &&
            (offset == Input.Length || char.IsWhiteSpace(Input[offset]));
    }

    public string ReadRemaining() {
        string value = Input[offset..].Trim().ToString();
        offset = Input.Length;
        return value;
    }

    public static string Text(ReadOnlySpan<char> token) {
        if (token.IsEmpty || token[0] != '"') {
            if (token.Contains('"')) throw new JsonException("Unexpected quote in command argument.");
            return token.ToString();
        }
        if (token.Length < 2 || token[^1] != '"') throw new JsonException("Unterminated command argument.");
        if (!token.Contains('\\') && !token[1..^1].Contains('"')) return token[1..^1].ToString();
        return JsonSerializer.Deserialize<string>(token) ?? string.Empty;
    }
}
