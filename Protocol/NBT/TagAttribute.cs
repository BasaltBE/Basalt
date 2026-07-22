namespace Basalt.Protocol.Nbt;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TagAttribute : Attribute {
    public TagType Type;

    public TagAttribute(TagType type) {
        Type = type;
    }
}
