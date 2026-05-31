using Basalt.Protocol.Types;

namespace Basalt.Server.World.Dimension;

public struct BroadcastOptions
{
    public float Radius = 64f;
    public Vec3f? Center;
    public global::Basalt.Server.Entity.Entity[]? Except;

    public BroadcastOptions()
    {
    }
}







