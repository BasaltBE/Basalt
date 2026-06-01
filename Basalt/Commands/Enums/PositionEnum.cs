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
}
