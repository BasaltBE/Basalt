using System.Globalization;
using System.Numerics;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Commands.Enums;

public sealed class PositionEnum(bool integer = false)
    : CommandEnum(integer ? CommandParameterType.IntPosition : CommandParameterType.Position) {
    public override bool Parse(CommandContext context, ref CommandReader reader, out object? value) {
        value = null;
        Span<float> coordinates = stackalloc float[3];
        Span<char> modes = stackalloc char[3];
        Vec3? origin = context.Player?.Position;
        for (int axis = 0; axis < 3; axis++) {
            if (!reader.Read(out ReadOnlySpan<char> token))
                return false;
            char mode = token[0] is '~' or '^' ? token[0] : '\0';
            modes[axis] = mode;
            if (mode != '\0') {
                if (origin is null)
                    return false;
                token = token[1..];
            }
            float number = 0;
            if ((!token.IsEmpty || mode == '\0') &&
                (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out number) || !float.IsFinite(number)))
                return false;
            if (integer && number != MathF.Truncate(number))
                return false;
            coordinates[axis] = number;
        }

        Vector3 position = new(coordinates[0], coordinates[1], coordinates[2]);
        if (modes.Contains('^')) {
            if (modes[0] != '^' || modes[1] != '^' || modes[2] != '^' || context.Player is not { } player)
                return false;
            float yaw = player.Rotation.Y * MathF.PI / 180;
            float pitch = player.Rotation.X * MathF.PI / 180;
            Vector3 forward = new(-MathF.Sin(yaw) * MathF.Cos(pitch), -MathF.Sin(pitch), MathF.Cos(yaw) * MathF.Cos(pitch));
            Vector3 left = new(MathF.Cos(yaw), 0, MathF.Sin(yaw));
            Vector3 up = Vector3.Cross(forward, left);
            position = new Vector3(origin!.X, origin.Y, origin.Z) + left * position.X + up * position.Y + forward * position.Z;
        }
        else if (origin is not null) {
            if (modes[0] == '~') position.X += origin.X;
            if (modes[1] == '~') position.Y += origin.Y;
            if (modes[2] == '~') position.Z += origin.Z;
        }

        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z) ||
            integer && (position.X < int.MinValue || position.X >= 2147483648f ||
                position.Y < int.MinValue || position.Y >= 2147483648f || position.Z < int.MinValue || position.Z >= 2147483648f))
            return false;
        value = integer
            ? new BlockPos { X = (int)MathF.Floor(position.X), Y = (int)MathF.Floor(position.Y), Z = (int)MathF.Floor(position.Z) }
            : new Vec3 { X = position.X, Y = position.Y, Z = position.Z };
        return true;
    }
}
