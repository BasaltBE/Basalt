namespace Basalt.Core.Commands;

using System.Globalization;

public sealed class XpAmountEnum : CommandEnum {
    public int Value { get; private set; }
    public bool Levels { get; private set; }

    public XpAmountEnum() : base("xp_amount") {
    }

    public override bool Parse(CommandContext ctx, string[] tokens, ref int tokenIndex) {
        if (tokenIndex >= tokens.Length) return false;

        string token = tokens[tokenIndex];
        Levels = token.EndsWith('L') || token.EndsWith('l');
        if (Levels) token = token[..^1];

        if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)) {
            return false;
        }

        Value = value;
        tokenIndex++;
        return true;
    }
}
