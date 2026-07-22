namespace Basalt.Core.Commands;

using Basalt.Protocol.Types;

public sealed class PositionEnum : CommandEnum {
    public Vec3f Value { get; private set; }

    public PositionEnum() : base("position") { }

    public override bool Parse(CommandContext ctx, string[] tokens, ref int tokenIndex) {
        if (tokenIndex + 2 >= tokens.Length)
            return false;

        Vec3f origin = ctx.Sender.AsPlayer()?.Location ?? new Vec3f();

        if (!ParseComponent(tokens[tokenIndex], origin.X, out float x) ||
            !ParseComponent(tokens[tokenIndex + 1], origin.Y, out float y) ||
            !ParseComponent(tokens[tokenIndex + 2], origin.Z, out float z))
            return false;

        Value = new Vec3f { X = x, Y = y, Z = z };
        tokenIndex += 3;
        return true;
    }

    public static bool ParseComponent(string token, float origin, out float value) {
        value = 0f;
        if (token == "~") {
            value = origin;
            return true;
        }
        if (token.StartsWith('~')) {
            if (token.Length == 1) {
                value = origin;
                return true;
            }
            if (!float.TryParse(token[1..], out float offset))
                return false;
            value = origin + offset;
            return true;
        }
        return float.TryParse(token, out value);
    }
}
