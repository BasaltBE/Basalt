using Basalt.Protocol.Enums;

namespace Basalt.Protocol.Packets;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PacketAttribute : Attribute {
    public PacketId Id;

    public PacketAttribute(PacketId id) {
        Id = id;
    }
}
