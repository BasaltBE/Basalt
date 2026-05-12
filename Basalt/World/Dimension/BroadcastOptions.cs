using Basalt.Protocol.Types;

namespace Basalt.World.Dimension;

public struct BroadcastOptions
{
    public float Radius = 64f;
    public Vec3f? Center;
    public global::Basalt.Entity.Entity[]? Except;

    public BroadcastOptions()
    {
    }
}
