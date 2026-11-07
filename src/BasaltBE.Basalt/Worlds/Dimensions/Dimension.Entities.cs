using System.Diagnostics;
using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Worlds.Providers.Types;
using BasaltBE.Logging;
using BasaltBE.Nbt;
using BasaltBE.Nbt.Types;
using NbtCodec = BasaltBE.Nbt.Nbt;

namespace BasaltBE.Basalt.Worlds.Dimensions;

public sealed partial class Dimension {
    static readonly EntityRegistry EntityStorage = new();
    readonly Dictionary<long, EntitySave> entitySaves = [];
    readonly Queue<long> entitySaveQueue = new();
    readonly Queue<EntityLoad> entityLoads = new();
    readonly HashSet<string> unavailableEntities = new(StringComparer.Ordinal);
    int inFlightEntities;

    sealed class EntityLoad(ChunkStorageKey key, EntityStorageRecord[] records) {
        internal readonly ChunkStorageKey Key = key;
        internal readonly EntityStorageRecord[] Records = records;
        internal int Index;
    }

    sealed class EntitySave(Entity entity) {
        internal readonly Entity Entity = entity;
        internal ChunkStorageKey? StoredChunk;
        internal bool Queued;
        internal bool InFlight;
        internal bool Pending;
        internal int Failures;
        internal ulong RetryTick;
    }

    internal void QueueEntitySaves() {
        if (World?.Storage is not { SupportsEntities: true })
            return;
        foreach ((long id, EntitySave state) in entitySaves) {
            state.Pending = true;
            state.Failures = 0;
            if (state.Queued || state.InFlight)
                continue;
            state.Queued = true;
            entitySaveQueue.Enqueue(id);
        }
    }

    void LoadEntities(bool flush = false) {
        long deadline = flush ? long.MaxValue : Stopwatch.GetTimestamp() + Stopwatch.Frequency / 1000;
        int remaining = flush ? int.MaxValue : 64;
        while (remaining-- > 0 && Stopwatch.GetTimestamp() < deadline && entityLoads.TryPeek(out EntityLoad? batch)) {
            EntityStorageRecord record = batch.Records[batch.Index++];
            ChunkStorageKey key = batch.Key;
            try {
                if (entitySaves.ContainsKey(record.Id))
                    continue;
                int offset = 0;
                if (NbtCodec.Read(record.Data, ref offset, Options.LittleEndian, new NbtReadLimits(64, 65536, 65536, 1024 * 1024)) is not CompoundTag tag || offset != record.Data.Length)
                    throw new InvalidDataException("Actor NBT is invalid.");
                string? identifier = tag.Get<StringTag>("identifier")?.Value;
                if (identifier is not null && identifier is not "minecraft:item" and not "minecraft:falling_block" && EntityStorage.Find(identifier) is null) {
                    if (unavailableEntities.Add(identifier))
                        Logger.Scoped("Storage").Warn($"Could not load entity type {identifier} in {Identifier}; stored actors were retained.");
                    continue;
                }
                Entity entity = EntityStorage.Load(tag);
                entity.StorageId = record.Id;
                entity.StorageOwner = this;
                if (((int)MathF.Floor(entity.Position.X) >> 4) != key.X || ((int)MathF.Floor(entity.Position.Z) >> 4) != key.Z)
                    throw new InvalidDataException("Actor position does not match its chunk index.");
                if (entity.GetTrait<BasaltBE.Basalt.Entities.Traits.EntityHealthTrait>() is { Current: <= 0 })
                    entity.Dead = true;
                entitySaves.Add(record.Id, new EntitySave(entity) { StoredChunk = key });
                AddEntity(entity);
            }
            catch (Exception error) when (error is InvalidDataException or KeyNotFoundException or ArgumentException or OverflowException or IndexOutOfRangeException or FormatException) {
                Logger.Scoped("Storage").Warn($"Could not load actor {record.Id} in {Identifier}: {error.Message}");
            }
            finally {
                World?.Storage?.ReleaseRead(record.Data.Length);
                batch.Records[batch.Index - 1] = default;
                if (batch.Index == batch.Records.Length)
                    entityLoads.Dequeue();
            }
        }
    }

    void SaveEntities(bool flush = false) {
        if (entitySaveQueue.Count == 0 || World?.Storage is not { SupportsEntities: true } storage)
            return;
        long deadline = flush ? long.MaxValue : Stopwatch.GetTimestamp() + Stopwatch.Frequency / 1000;
        int remaining = flush ? entitySaveQueue.Count : Math.Min(64, entitySaveQueue.Count);
        List<EntityStorageChange> changes = new(Math.Min(64, remaining));
        int bytes = 0;
        while (remaining-- > 0 && Stopwatch.GetTimestamp() < deadline) {
            long id = entitySaveQueue.Dequeue();
            if (!entitySaves.TryGetValue(id, out EntitySave? state))
                continue;
            state.Queued = false;
            if (state.InFlight)
                continue;
            if (!flush && TickValue < state.RetryTick) {
                state.Queued = true;
                entitySaveQueue.Enqueue(id);
                continue;
            }
            Entity entity = state.Entity;
            ChunkStorageKey? current = null;
            byte[]? data = null;
            try {
                if (entity.SpawnedDimension == this && entity.StorageId == id && !entity.Dead) {
                    current = new ChunkStorageKey(Type, Identifier, PrimaryStorageDimension,
                        (int)MathF.Floor(entity.Position.X) >> 4, (int)MathF.Floor(entity.Position.Z) >> 4);
                    CompoundTag tag = EntityStorage.Save(entity);
                    int length = tag.GetSize(Options.LittleEndian);
                    if (length > 1024 * 1024)
                        throw new InvalidDataException("Actor NBT exceeds 1 MiB.");
                    data = new byte[length];
                    int offset = 0;
                    NbtCodec.Write(tag, data, ref offset, Options.LittleEndian);
                }
                else if (state.StoredChunk is null) {
                    entitySaves.Remove(id);
                    continue;
                }
            }
            catch (Exception error) when (error is InvalidDataException or ArgumentException or OverflowException) {
                Logger.Scoped("Storage").Error($"Could not save actor {id} in {Identifier}: {error.Message}");
                state.Failures++;
                state.Pending = true;
                continue;
            }
            state.InFlight = true;
            inFlightEntities++;
            state.Pending = false;
            changes.Add(new EntityStorageChange(id, state.StoredChunk, current, data));
            bytes += data?.Length ?? 0;
            if (changes.Count >= 64 || bytes >= 4 * 1024 * 1024)
                break;
        }
        if (changes.Count == 0)
            return;
        EntityStorageChange[] snapshot = changes.ToArray();
        if (storage.WriteEntities(this, snapshot))
            return;
        foreach (EntityStorageChange change in snapshot) {
            EntitySave state = entitySaves[change.Id];
            state.InFlight = false;
            inFlightEntities--;
            state.Pending = true;
            state.Queued = true;
            entitySaveQueue.Enqueue(change.Id);
        }
    }

    void CompleteEntityWrite(WorldStorageCompletion completion) {
        if (completion.EntityChanges is not { } changes)
            return;
        foreach (EntityStorageChange change in changes) {
            if (!entitySaves.TryGetValue(change.Id, out EntitySave? state)) {
                foreach (Dimension dimension in World!.Dimensions.Values) {
                    if (dimension == this || !dimension.entitySaves.ContainsKey(change.Id))
                        continue;
                    dimension.CompleteEntityWrite(completion with { EntityChanges = [change] });
                    break;
                }
                continue;
            }
            state.InFlight = false;
            inFlightEntities--;
            if (completion.Error is not null) {
                state.Failures++;
                state.RetryTick = TickValue + 20;
                state.Pending = true;
            }
            else {
                state.Failures = 0;
                state.StoredChunk = change.Current;
                if (change.Current is null && (state.Entity.SpawnedDimension != this || state.Entity.StorageId != change.Id)) {
                    entitySaves.Remove(change.Id);
                    continue;
                }
            }
            if (state.Pending && !state.Queued && state.Failures < 3) {
                state.Queued = true;
                entitySaveQueue.Enqueue(change.Id);
            }
        }
    }
}
