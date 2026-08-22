using Basalt.Core.Enums;
using Basalt.BedrockProtocol.Types;

namespace Basalt.Core.Entities.Metadata;

public sealed class EntityActorMetadata {
    private readonly Entity _entity;
    private readonly Dictionary<ActorDataId, ActorDataItem> _metadata = [];

    public EntityActorMetadata(Entity entity) {
        _entity = entity;
    }

    public bool HasActorMetadata(ActorDataId id) => _metadata.ContainsKey(id);

    public ActorDataItem? GetActorMetadata(ActorDataId id) {
        return _metadata.TryGetValue(id, out ActorDataItem? payload) ? payload : null;
    }

    public void SetActorMetadata(ActorDataId id, ActorDataItem payload) {
        ArgumentNullException.ThrowIfNull(payload);

        bool changed = !_metadata.TryGetValue(id, out ActorDataItem? previous) || !Equals(previous.Value, payload.Value) ||
            previous.Type != payload.Type;
        _metadata[id] = payload;

        if (changed) {
            _entity.SendActorMetadataUpdate(id, payload);
        }
    }

    public List<ActorDataItem> GetAll() {
        List<ActorDataItem> metadata = new(_metadata.Count);
        foreach ((ActorDataId id, ActorDataItem payload) in _metadata.OrderBy(static entry => entry.Key)) {
            payload.Id = (uint)id;
            metadata.Add(payload);
        }

        return metadata;
    }
}
