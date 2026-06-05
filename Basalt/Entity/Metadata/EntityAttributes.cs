namespace Basalt.Server.Entity.Metadata;

using Basalt.Protocol.Enums;
using ProtoAttribute = Basalt.Protocol.Types.Attribute;


public sealed class EntityAttributes
{
    private readonly Dictionary<AttributeName, ProtoAttribute> _attributes = [];

    public IReadOnlyList<ProtoAttribute> GetAll()
    {
        return _attributes.Values.ToList();
    }

    public bool HasAttribute(AttributeName name)
    {
        return _attributes.ContainsKey(name);
    }

    public ProtoAttribute? GetAttribute(AttributeName name)
    {
        return _attributes.TryGetValue(name, out ProtoAttribute? attribute) ? attribute : null;
    }

    public void SetAttribute(ProtoAttribute attribute)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        _attributes[attribute.Name] = attribute;
    }

    public bool RemoveAttribute(AttributeName name)
    {
        return _attributes.Remove(name);
    }
}






