namespace Basalt.Server.Commands;

using Basalt.Protocol.Types;

public sealed class PositionEnum : CommandEnum
{
    public Vec3f Value;

    public PositionEnum() : base("position")
    {
        Value = new Vec3f();
    }

    public PositionEnum(Vec3f value) : base("position")
    {
        Value = value;
    }

    public override bool Parse(CommandExecutionState state, CommandParameter parameter, string[] tokens, ref int tokenIndex)
    {
        if (tokenIndex + 2 >= tokens.Length)
        {
            return false;
        }

        Vec3f origin = state.Executor is PlayerExecutor executor ? executor.Player.Position : new Vec3f();
        if (!Parse(tokens, tokenIndex, origin, out Vec3f position))
        {
            return false;
        }

        Value = position;
        tokenIndex += 3;
        return true;
    }

    public static bool ParseComponent(string token, float origin, out float value)
    {
        value = 0f;
        if (token == "~")
        {
            value = origin;
            return true;
        }

        if (token.StartsWith('~'))
        {
            string offset = token[1..];
            if (offset.Length == 0)
            {
                value = origin;
                return true;
            }

            if (!float.TryParse(offset, out float step))
            {
                return false;
            }

            value = origin + step;
            return true;
        }

        return float.TryParse(token, out value);
    }

    public static bool Parse(string[] tokens, int start, Vec3f origin, out Vec3f position)
    {
        position = new Vec3f();
        if (start + 2 >= tokens.Length)
        {
            return false;
        }

        if (!ParseComponent(tokens[start], origin.X, out float x) ||
            !ParseComponent(tokens[start + 1], origin.Y, out float y) ||
            !ParseComponent(tokens[start + 2], origin.Z, out float z))
        {
            return false;
        }

        position = new Vec3f { X = x, Y = y, Z = z };
        return true;
    }
}
