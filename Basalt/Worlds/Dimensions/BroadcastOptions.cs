using Basalt.Protocol.Types;

namespace Basalt.Core.Worlds.Dimensions;

public struct BroadcastOptions
{
    public float Radius = 64f;
    public Vec3f? Center;
    public Entities.Entity[]? Except;

    public BroadcastOptions()
    {
    }
}







