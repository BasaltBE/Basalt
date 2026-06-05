using Basalt.Protocol.Types;

namespace Basalt.Core.World.Dimension;

public struct BroadcastOptions
{
    public float Radius = 64f;
    public Vec3f? Center;
    public global::Basalt.Core.Entity.Entity[]? Except;

    public BroadcastOptions()
    {
    }
}







