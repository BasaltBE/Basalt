namespace BasaltBE.Protocol.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PacketIdAttribute(int id) : Attribute {
    public readonly int Id = id;
}
