namespace Basalt.BedrockProtocol;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PacketIdAttribute : Attribute {
    public readonly int Id;

    public PacketIdAttribute(int id) {
        Id = id;
    }
}
