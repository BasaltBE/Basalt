namespace Basalt.Core.Entities.Metadata;

using Basalt.Protocol.Enums;
using Basalt.Protocol.Packets;
using Basalt.Core.Worlds;
using ProtoAttribute = Basalt.Protocol.Types.Attribute;


public sealed class EntityAttributes
{
    private readonly Dictionary<AttributeName, ProtoAttribute> _attributes = [];
    private readonly Entity _entity;

    internal EntityAttributes(Entity entity)
    {
        _entity = entity;
    }

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

    /// <summary>
    /// Sends current attributes to the owning player's client.
    /// No-op if the entity is not a connected player.
    /// </summary>
    public void Send()
    {
        if (_entity is not Player.Player player)
        {
            return;
        }

        if (player.Network is null || player.Connection is null)
        {
            return;
        }

        ulong tick = player.Dimension?.World is Tickable tickable ? tickable.TickValue : 0;

        UpdateAttributesPacket packet = new()
        {
            RuntimeId = player.RuntimeId,
            Tick = tick,
            Attributes = _attributes.Values.ToList()
        };

        if (packet.Attributes.Count > 0)
        {
            player.Network.SendPacket(player.Connection, packet);
        }

        player.AttributesDirty = false;
    }
}






