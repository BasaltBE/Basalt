using Basalt.Protocol.Enums;

namespace Basalt.Entity;

public sealed class EntityActorMetadata
{
    private readonly Entity _entity;
    private readonly Dictionary<ActorDataId, (ActorDataType Type, object Value)> _metadata = [];

    public EntityActorMetadata(Entity entity)
    {
        _entity = entity;
    }

    public bool HasActorMetadata(ActorDataId id)
    {
        return _metadata.ContainsKey(id);
    }

    public T? GetActorMetadata<T>(ActorDataId id, ActorDataType type)
    {
        if (!_metadata.TryGetValue(id, out (ActorDataType Type, object Value) value))
        {
            return default;
        }

        if (value.Type != type)
        {
            return default;
        }

        if (value.Value is T typed)
        {
            return typed;
        }

        return default;
    }

    public void SetActorMetadata(ActorDataId id, ActorDataType type, object value)
    {
        bool changed = true;
        if (_metadata.TryGetValue(id, out (ActorDataType Type, object Value) previous))
        {
            changed = previous.Type != type || !Equals(previous.Value, value);
        }

        _metadata[id] = (type, value);
        if (changed)
        {
            _entity.SendActorMetadataUpdate(id, type, value);
        }
    }
}
