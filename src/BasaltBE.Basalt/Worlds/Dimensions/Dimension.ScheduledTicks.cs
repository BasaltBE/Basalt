using BasaltBE.Basalt.Blocks;
using BasaltBE.Basalt.Worlds.Dimensions.Chunks;
using BasaltBE.Nbt.Types;
using BasaltBE.Logging;

namespace BasaltBE.Basalt.Worlds.Dimensions;

public sealed partial class Dimension {
    const int ScheduledUpdateLimit = 256;
    readonly Dictionary<long, ScheduledChunk> scheduledChunks = [];
    readonly Dictionary<BlockPermutation, CompoundTag> scheduledBlockStates = [];
    readonly PriorityQueue<(long Key, int Generation), (ulong Tick, long Order, long Key)> scheduledChunkHeads = new();
    readonly List<(long Key, ScheduledChunk State)> dispatchedScheduledChunks = [];
    long nextScheduledTickOrder;
    int overdueScheduledChunks;
    ulong oldestScheduledTickOverdue;

    public int OverdueScheduledChunks => Volatile.Read(ref overdueScheduledChunks);
    public ulong OldestScheduledTickOverdue => Volatile.Read(ref oldestScheduledTickOverdue);

    sealed class ScheduledTick(CompoundTag record, int x, int y, int z, BlockPermutation permutation, long due, long order) {
        internal readonly CompoundTag Record = record;
        internal readonly int X = x;
        internal readonly int Y = y;
        internal readonly int Z = z;
        internal readonly BlockPermutation Permutation = permutation;
        internal readonly long Due = due;
        internal readonly long Order = order;
    }

    void UpdateScheduledTickBacklog() {
        int overdueChunks = 0;
        ulong oldestOverdue = 0;
        foreach (var item in scheduledChunkHeads.UnorderedItems) {
            if (!scheduledChunks.TryGetValue(item.Element.Key, out ScheduledChunk? state) ||
                !state.Active || state.Generation != item.Element.Generation ||
                state.QueuedTick != item.Priority.Tick || item.Priority.Tick > TickValue)
                continue;

            overdueChunks++;
            oldestOverdue = Math.Max(oldestOverdue, TickValue - item.Priority.Tick);
        }

        Volatile.Write(ref overdueScheduledChunks, overdueChunks);
        Volatile.Write(ref oldestScheduledTickOverdue, oldestOverdue);
    }

    sealed class ScheduledChunk {
        internal readonly SortedSet<ScheduledTick> Ticks = new(Comparer<ScheduledTick>.Create(static (left, right) => {
            int due = left.Due.CompareTo(right.Due);
            return due != 0 ? due : left.Order.CompareTo(right.Order);
        }));
        internal readonly Dictionary<(int X, int Y, int Z), List<ScheduledTick>> TicksByPosition = [];
        internal HashSet<CompoundTag>? CanceledRecords;
        internal long Clock;
        internal long ClockOffset;
        internal ulong ActivatedAt;
        internal ulong? QueuedTick;
        internal long QueueOrder;
        internal int Generation;
        internal bool Active;
        internal bool RecordChangesPending;
        internal bool HasUnindexedRecords;
    }

    void LoadScheduledTicks(Chunk chunk) {
        if (chunk.PendingTicksInvalid || chunk.PendingTicks is not { } pending) return;

        IntTag? currentTick = pending.Get<IntTag>("currentTick");
        ListTag? list = pending.Get<ListTag>("tickList");
        if (currentTick is null || list is null) {
            chunk.PendingTicksInvalid = true;
            Logger.Scoped("Storage").Warn(
                $"Could not index pending ticks in {Identifier} chunk ({chunk.X}, {chunk.Z}): invalid currentTick or tickList."
            );
            return;
        }
        if (currentTick.Value < 0) {
            chunk.PendingTicksInvalid = true;
            Logger.Scoped("Storage").Warn(
                $"Could not index pending ticks in {Identifier} chunk ({chunk.X}, {chunk.Z}): negative currentTick."
            );
            return;
        }

        long key = GetKey(chunk.X, chunk.Z);
        ScheduledChunk state = new() { Clock = currentTick.Value };
        string? invalidRecordReason = null;
        int invalidRecordCount = 0;
        if (chunk.PendingTicksRecovered) {
            chunk.PendingTicksRecovered = false;
            chunk.MarkDirty();
        }

        foreach (Tag tag in list.Value) {
            if (tag is not CompoundTag record) continue;

            LongTag? time = record.Get<LongTag>("time");
            if (time is null ||
                record.Get<IntTag>("x") is not { } x ||
                record.Get<IntTag>("y") is not { } y ||
                record.Get<IntTag>("z") is not { } z ||
                record.Get<CompoundTag>("blockState") is not { } blockState ||
                blockState.Get<StringTag>("name") is not { } name) {
                state.HasUnindexedRecords |= time is { Value: >= 0 };
                invalidRecordReason ??= "malformed tick record";
                invalidRecordCount++;
                continue;
            }

            if (time.Value == -1) continue;
            if (time.Value < 0) {
                invalidRecordReason ??= "invalid tick time";
                invalidRecordCount++;
                continue;
            }
            if ((x.Value >> 4) != chunk.X || (z.Value >> 4) != chunk.Z ||
                y.Value < MinimumY || y.Value > MaximumY) {
                state.HasUnindexedRecords = true;
                invalidRecordReason ??= "tick coordinates are outside the chunk or dimension";
                invalidRecordCount++;
                continue;
            }
            if (!BlockPalette.HasScheduledUpdates(name.Value)) {
                state.HasUnindexedRecords = true;
                continue;
            }
            if (BlockStorage.ReadPermutation(blockState) is not { } permutation) {
                state.HasUnindexedRecords = true;
                invalidRecordReason ??= $"invalid block state for {name.Value}";
                invalidRecordCount++;
                continue;
            }

            long due = Math.Max(time.Value, state.Clock);
            ScheduledTick scheduled = new(record, x.Value, y.Value, z.Value, permutation, due, ++nextScheduledTickOrder);
            state.Ticks.Add(scheduled);
            var position = (scheduled.X, scheduled.Y, scheduled.Z);
            if (!state.TicksByPosition.TryGetValue(position, out List<ScheduledTick>? positionTicks)) {
                positionTicks = [];
                state.TicksByPosition.Add(position, positionTicks);
            }
            int insert = positionTicks.BinarySearch(scheduled, state.Ticks.Comparer);
            positionTicks.Insert(insert < 0 ? ~insert : insert, scheduled);
        }

        if (invalidRecordReason is not null) {
            Logger.Scoped("Storage").Warn(
                $"Found {invalidRecordCount} invalid pending tick records in {Identifier} chunk ({chunk.X}, {chunk.Z}); preserved them. First issue: {invalidRecordReason}."
            );
        }
        if (state.Ticks.Count == 0 && !state.HasUnindexedRecords) return;

        scheduledChunks[key] = state;
        if (simulatedChunks.Contains(key)) ActivateScheduledTicks(key, state);
    }

    void ActivateScheduledTicks(long key, ScheduledChunk state) {
        if (state.Active) return;

        state.Active = true;
        state.ActivatedAt = TickValue;
        QueueScheduledChunk(key, state);
    }

    void DeactivateScheduledTicks(long key, ScheduledChunk state) {
        if (!state.Active) return;

        long clock = ScheduledClock(state);
        if (state.Clock != clock && chunks.TryGetValue(key, out Chunk? chunk) && chunk is not null) chunk.MarkDirty();
        state.Clock = clock;
        state.Active = false;
        state.QueuedTick = null;
        state.Generation++;
    }

    long ScheduledClock(ScheduledChunk state) {
        ulong elapsed = state.Active ? TickValue - state.ActivatedAt : 0;
        return elapsed > (ulong)(long.MaxValue - state.Clock) ? long.MaxValue : state.Clock + (long)elapsed;
    }

    void QueueScheduledChunk(long key, ScheduledChunk state) {
        if (!state.Active || state.Ticks.Count == 0) return;

        long remaining = Math.Max(0, state.Ticks.Min!.Due - ScheduledClock(state));
        ulong dueTick = (ulong)remaining > ulong.MaxValue - TickValue ? ulong.MaxValue : TickValue + (ulong)remaining;
        if (state.QueuedTick is { } queuedTick && queuedTick <= dueTick) return;

        state.QueuedTick = dueTick;
        state.Generation++;
        scheduledChunkHeads.Enqueue((key, state.Generation), (dueTick, state.QueueOrder, key));

        if (scheduledChunkHeads.Count > scheduledChunks.Count * 4 + 64) {
            scheduledChunkHeads.Clear();
            foreach ((long chunkKey, ScheduledChunk scheduled) in scheduledChunks) {
                if (scheduled.Active && scheduled.Ticks.Count > 0) {
                    long delay = Math.Max(0, scheduled.Ticks.Min!.Due - ScheduledClock(scheduled));
                    ulong tick = (ulong)delay > ulong.MaxValue - TickValue ? ulong.MaxValue : TickValue + (ulong)delay;
                    scheduledChunkHeads.Enqueue((chunkKey, scheduled.Generation), (tick, scheduled.QueueOrder, chunkKey));
                }
            }
        }
    }

    internal void ScheduleBlockUpdate(int x, int y, int z, ulong delay, BlockPermutation? scheduledPermutation = null) {
        if (y < MinimumY || y > MaximumY || GetChunk(x >> 4, z >> 4) is not { } chunk) return;
        if (chunk.PendingTicksInvalid) return;

        BlockPermutation current = chunk.GetPermutation(x & 15, y, z & 15);
        if (!BlockPalette.HasScheduledUpdates(current.Identifier)) return;

        BlockPermutation permutation = scheduledPermutation ?? current;
        if (!BlockPalette.HasScheduledUpdates(permutation.Identifier)) return;

        long key = GetKey(chunk.X, chunk.Z);
        if (!scheduledChunks.TryGetValue(key, out ScheduledChunk? state)) {
            long clock = chunk.PendingTicks?.Get<IntTag>("currentTick") is { } savedClock
                ? Math.Max(0, savedClock.Value)
                : (long)Math.Min(TickValue, (ulong)long.MaxValue);
            state = new ScheduledChunk { Clock = clock };
            scheduledChunks.Add(key, state);
        }

        if (simulatedChunks.Contains(key) && !state.Active) {
            state.Active = true;
            state.ActivatedAt = TickValue;
        }

        long now = ScheduledClock(state);
        long wait = Math.Max(1, (long)Math.Min(delay, (ulong)long.MaxValue));
        long due = wait > long.MaxValue - now ? long.MaxValue : now + wait;
        (int X, int Y, int Z) position = (x, y, z);
        ScheduledTick? existing = null;
        state.TicksByPosition.TryGetValue(position, out List<ScheduledTick>? positionTicks);
        if (scheduledPermutation is not null && positionTicks is not null) {
            foreach (ScheduledTick pending in positionTicks) {
                if (pending.Due != due || pending.Permutation != permutation) continue;
                if (positionTicks[^1] == pending) return;

                existing = pending;
                state.Ticks.Remove(pending);
                positionTicks.Remove(pending);
                break;
            }
        }
        if (existing is null && scheduledPermutation is not null && positionTicks is { Count: > 0 } &&
            positionTicks[^1].Permutation.Hash == permutation.Hash)
            return;
        if (scheduledPermutation is null && positionTicks is { Count: > 0 }) {
            ScheduledTick previous = positionTicks[0];
            existing = previous;
            if (previous.Due <= due) return;

            state.Ticks.Remove(previous);
            positionTicks.RemoveAt(0);
            if (positionTicks.Count == 0) state.TicksByPosition.Remove(position);
        }

        CompoundTag record;
        if (existing is null) {
            record = new CompoundTag();
            if (!scheduledBlockStates.TryGetValue(permutation, out CompoundTag? blockState)) {
                blockState = BlockStorage.CreatePermutationTag(permutation);
                scheduledBlockStates.Add(permutation, blockState);
            }
            record.Add("blockState", blockState);
            record.Add("time", new LongTag { Value = due });
            record.Add("x", new IntTag { Value = x });
            record.Add("y", new IntTag { Value = y });
            record.Add("z", new IntTag { Value = z });
        } else {
            record = existing.Record;
            record.Get<LongTag>("time")!.Value = due;
            if (existing.Permutation != permutation && record.Get<CompoundTag>("blockState") is { } blockState) {
                CompoundTag updated = BlockStorage.CreatePermutationTag(permutation);
                foreach ((string name, Tag tag) in blockState.Tags) {
                    if (name is not ("name" or "states" or "version")) updated.Add(name, tag);
                }
                if (permutation.Identifier == existing.Permutation.Identifier &&
                    blockState.Get<CompoundTag>("states") is { } previousStates &&
                    updated.Get<CompoundTag>("states") is { } updatedStates) {
                    foreach ((string name, Tag tag) in previousStates.Tags) {
                        if (!updatedStates.Tags.ContainsKey(name)) updatedStates.Add(name, tag);
                    }
                }
                record.Add("blockState", updated);
            }
        }
        if (chunk.PendingTicks is null) {
            chunk.PendingTicks = new CompoundTag();
            chunk.PendingTicks.Add("currentTick", new IntTag {
                Value = (int)Math.Min(state.Clock, int.MaxValue)
            });
        }
        if (chunk.PendingTicks.Get<IntTag>("currentTick") is null)
            chunk.PendingTicks.Add("currentTick", new IntTag {
                Value = (int)Math.Min(state.Clock, int.MaxValue)
            });
        if (chunk.PendingTicks.Get<ListTag>("tickList") is not { } tickList) {
            tickList = new ListTag { ElementType = BasaltBE.Nbt.Enums.TagType.Compound };
            chunk.PendingTicks.Add("tickList", tickList);
        }
        if (existing is null) tickList.Value.Add(record);
        else if (scheduledPermutation is not null) {
            tickList.Value.Remove(record);
            tickList.Value.Add(record);
        }
        ScheduledTick scheduled = new(record, x, y, z, permutation, due,
            scheduledPermutation is not null ? ++nextScheduledTickOrder : existing?.Order ?? ++nextScheduledTickOrder);
        state.Ticks.Add(scheduled);
        if (!state.TicksByPosition.TryGetValue(position, out positionTicks)) {
            positionTicks = [];
            state.TicksByPosition.Add(position, positionTicks);
        }
        int insert = positionTicks.BinarySearch(scheduled, state.Ticks.Comparer);
        positionTicks.Insert(insert < 0 ? ~insert : insert, scheduled);
        chunk.MarkDirty();
        QueueScheduledChunk(key, state);
    }

    void CancelScheduledBlockUpdate(int x, int y, int z) {
        long key = GetKey(x >> 4, z >> 4);
        if (!scheduledChunks.TryGetValue(key, out ScheduledChunk? state)) return;

        if (!state.TicksByPosition.Remove((x, y, z), out List<ScheduledTick>? canceled)) return;

        Chunk? chunk = GetChunk(x >> 4, z >> 4);
        foreach (ScheduledTick scheduled in canceled) {
            state.Ticks.Remove(scheduled);
            (state.CanceledRecords ??= []).Add(scheduled.Record);
        }
        if (chunk is not null) chunk.MarkDirty();
        state.QueuedTick = null;
        state.Generation++;
        if (state.Ticks.Count == 0) {
            if (chunk?.PendingTicks is null) scheduledChunks.Remove(key);
        } else {
            QueueScheduledChunk(key, state);
        }
    }

    void MoveBlockTicks(ReadOnlySpan<PistonMove> moves) {
        List<(PistonMove Move, ScheduledTick Tick, long Remaining)>? transfer = null;
        foreach (PistonMove move in moves) {
            long sourceKey = GetKey(move.Source.X >> 4, move.Source.Z >> 4);
            if (!scheduledChunks.TryGetValue(sourceKey, out ScheduledChunk? sourceState) ||
                !sourceState.TicksByPosition.TryGetValue(move.Source, out List<ScheduledTick>? ticks))
                continue;

            long sourceClock = ScheduledClock(sourceState);
            foreach (ScheduledTick tick in ticks) (transfer ??= []).Add((move, tick, Math.Max(0, tick.Due - sourceClock)));
        }

        if (transfer is null) return;

        HashSet<long> updatedChunks = [];
        foreach (var (move, tick, _) in transfer) {
            long sourceKey = GetKey(move.Source.X >> 4, move.Source.Z >> 4);
            ScheduledChunk sourceState = scheduledChunks[sourceKey];
            sourceState.Ticks.Remove(tick);
            if (sourceState.TicksByPosition.TryGetValue(move.Source, out List<ScheduledTick>? sourceTicks)) {
                sourceTicks.Remove(tick);
                if (sourceTicks.Count == 0) sourceState.TicksByPosition.Remove(move.Source);
            }
            if (move.SourceChunk.PendingTicks?.Get<ListTag>("tickList") is { } sourceTickList)
                sourceTickList.Value.Remove(tick.Record);
            if (move.SourceChunk.PendingTicks is { } sourcePending &&
                sourcePending.Get<ListTag>("tickList") is { Value.Count: 0 } &&
                sourcePending.Tags.Count == 2)
                move.SourceChunk.PendingTicks = null;

            move.SourceChunk.MarkDirty();
            sourceState.QueuedTick = null;
            sourceState.Generation++;
            updatedChunks.Add(sourceKey);
        }

        foreach ((PistonMove move, ScheduledTick tick, long remaining) in transfer) {
            long destinationKey = GetKey(move.Destination.X >> 4, move.Destination.Z >> 4);
            if (!scheduledChunks.TryGetValue(destinationKey, out ScheduledChunk? destinationState)) {
                long clock = move.DestinationChunk.PendingTicks?.Get<IntTag>("currentTick") is { } savedClock
                    ? Math.Max(0, savedClock.Value)
                    : (long)Math.Min(TickValue, (ulong)long.MaxValue);
                destinationState = new ScheduledChunk { Clock = clock };
                scheduledChunks.Add(destinationKey, destinationState);
            }

            if (simulatedChunks.Contains(destinationKey) && !destinationState.Active) {
                destinationState.Active = true;
                destinationState.ActivatedAt = TickValue;
            }

            long destinationClock = ScheduledClock(destinationState);
            long due = remaining > long.MaxValue - destinationClock
                ? long.MaxValue
                : destinationClock + remaining;
            tick.Record.Get<LongTag>("time")!.Value = due;
            tick.Record.Get<IntTag>("x")!.Value = move.Destination.X;
            tick.Record.Get<IntTag>("y")!.Value = move.Destination.Y;
            tick.Record.Get<IntTag>("z")!.Value = move.Destination.Z;
            if (move.DestinationChunk.PendingTicks is null) {
                move.DestinationChunk.PendingTicks = new CompoundTag();
                move.DestinationChunk.PendingTicks.Add("currentTick", new IntTag {
                    Value = (int)Math.Min(destinationState.Clock, int.MaxValue)
                });
            }
            if (move.DestinationChunk.PendingTicks.Get<IntTag>("currentTick") is null)
                move.DestinationChunk.PendingTicks.Add("currentTick", new IntTag {
                    Value = (int)Math.Min(destinationState.Clock, int.MaxValue)
                });
            if (move.DestinationChunk.PendingTicks.Get<ListTag>("tickList") is not { } destinationTickList) {
                destinationTickList = new ListTag { ElementType = BasaltBE.Nbt.Enums.TagType.Compound };
                move.DestinationChunk.PendingTicks.Add("tickList", destinationTickList);
            }
            destinationTickList.Value.Add(tick.Record);

            ScheduledTick transferred = new(
                tick.Record,
                move.Destination.X,
                move.Destination.Y,
                move.Destination.Z,
                tick.Permutation,
                due,
                ++nextScheduledTickOrder
            );
            destinationState.Ticks.Add(transferred);
            (int X, int Y, int Z) destination = move.Destination;
            if (!destinationState.TicksByPosition.TryGetValue(destination, out List<ScheduledTick>? destinationTicks)) {
                destinationTicks = [];
                destinationState.TicksByPosition.Add(destination, destinationTicks);
            }
            int insert = destinationTicks.BinarySearch(transferred, destinationState.Ticks.Comparer);
            destinationTicks.Insert(insert < 0 ? ~insert : insert, transferred);
            destinationState.QueuedTick = null;
            destinationState.Generation++;
            move.DestinationChunk.MarkDirty();
            updatedChunks.Add(destinationKey);
        }

        foreach (long key in updatedChunks) {
            if (!scheduledChunks.TryGetValue(key, out ScheduledChunk? state)) continue;
            if (state.Ticks.Count == 0 && !state.HasUnindexedRecords &&
                chunks.TryGetValue(key, out Chunk? chunk) && chunk?.PendingTicks is null) {
                scheduledChunks.Remove(key);
                continue;
            }
            QueueScheduledChunk(key, state);
        }
    }

    internal bool HasPendingBlockUpdate(int x, int y, int z) =>
        scheduledChunks.TryGetValue(GetKey(x >> 4, z >> 4), out ScheduledChunk? state) &&
        state.TicksByPosition.ContainsKey((x, y, z));

    internal void PrepareScheduledTicks(Chunk chunk) {
        if (chunk.PendingTicksInvalid) return;

        long key = GetKey(chunk.X, chunk.Z);
        if (!scheduledChunks.TryGetValue(key, out ScheduledChunk? state)) return;

        if (chunk.PendingTicks is { } pending && state.CanceledRecords is { Count: > 0 } canceledRecords &&
            pending.Get<ListTag>("tickList") is { } tickList) {
            tickList.Value.RemoveAll(record => record is CompoundTag compound && canceledRecords.Contains(compound));
            state.CanceledRecords = null;
            if (pending.Tags.Count == 2 && tickList.Value.Count == 0) chunk.PendingTicks = null;
        }

        if (state.Ticks.Count == 0) {
            if (!state.HasUnindexedRecords) {
                scheduledChunks.Remove(key);
                return;
            }
        }
        if (chunk.PendingTicks is not { } currentPending) return;

        long clock = ScheduledClock(state);
        if (currentPending.Get<IntTag>("currentTick") is { } currentTick) currentTick.Value = (int)Math.Min(clock, int.MaxValue);
        else currentPending.Add("currentTick", new IntTag { Value = (int)Math.Min(clock, int.MaxValue) });

        if (clock > int.MaxValue && currentPending.Get<ListTag>("tickList") is { } pendingTicks) {
            long recordClock = state.ClockOffset == 0 ? clock : clock - state.ClockOffset;
            if (state.ClockOffset == 0) state.ClockOffset = clock - int.MaxValue;

            foreach (Tag tag in pendingTicks.Value) {
                if (tag is not CompoundTag record || record.Get<LongTag>("time") is not { Value: >= 0 } time) continue;

                long remaining = Math.Max(0, time.Value - recordClock);
                time.Value = remaining > long.MaxValue - int.MaxValue
                    ? long.MaxValue
                    : int.MaxValue + remaining;
            }
        }
    }
}
