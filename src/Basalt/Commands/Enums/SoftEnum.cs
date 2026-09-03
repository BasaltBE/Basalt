namespace Basalt.Core.Commands;

public abstract class SoftEnum : CommandEnum {
    public string? Value { get; private set; }

    protected SoftEnum(string identifier) : base(identifier) {
    }

    public override bool Parse(CommandContext ctx, string[] tokens, ref int tokenIndex) {
        if (tokenIndex >= tokens.Length) {
            return false;
        }

        Value = tokens[tokenIndex++];
        return true;
    }

    public abstract string[] GetOptions(Server server);
}
