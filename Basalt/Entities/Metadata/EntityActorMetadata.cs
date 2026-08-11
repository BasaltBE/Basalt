using Basalt.Core.Enums;
using BedrockProtocol.Types;

namespace Basalt.Core.Entities.Metadata;

public sealed class EntityActorMetadata {
    private readonly Entity _entity;

    private readonly Dictionary<ActorDataId, DataItemEntryPayloadVariant> _metadata = [];

    public EntityActorMetadata(Entity entity) {
        _entity = entity;
    }

    public bool HasActorMetadata(ActorDataId id) {
        return _metadata.ContainsKey(id);
    }

    public T? GetActorMetadata<T>(ActorDataId id)
        where T : class, DataItemEntryPayloadVariant {

        if (!_metadata.TryGetValue(id, out DataItemEntryPayloadVariant? payload)) {
            return null;
        }

        return payload as T;
    }

    public void SetActorMetadata(
        ActorDataId id,
        DataItemEntryPayloadVariant payload
    ) {
        ArgumentNullException.ThrowIfNull(payload);

        bool changed = true;

        if (_metadata.TryGetValue(id, out DataItemEntryPayloadVariant? previous)) {
            changed = !Equals(previous, payload);
        }

        _metadata[id] = payload;

        if (changed) {
            _entity.SendActorMetadataUpdate(id, payload);
        }
    }

    public List<DataItemEntry> GetAll() {
        List<DataItemEntry> metadata = new(_metadata.Count);

        foreach ((ActorDataId id, DataItemEntryPayloadVariant payload) in _metadata.OrderBy(static entry => entry.Key)) {
            metadata.Add(new DataItemEntry {
                ID = (uint)id,
                Payload = payload
            });
        }

        return metadata;
    }
}
