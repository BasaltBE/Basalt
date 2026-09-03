namespace Basalt.Core.Commands;

public sealed class StringEnum : CommandEnum {
    public string? Value { get; private set; }

    public StringEnum() : base("string") { }

    public override bool Parse(CommandContext ctx, string[] tokens, ref int tokenIndex) {
        if (tokenIndex >= tokens.Length)
            return false;

        string token = tokens[tokenIndex++];
        if (token.StartsWith('"')) {
            while (!token.EndsWith('"') && tokenIndex < tokens.Length)
                token += " " + tokens[tokenIndex++];

            if (!token.EndsWith('"'))
                return false;

            token = token[1..^1];
        }

        Value = token;
        return true;
    }
}
