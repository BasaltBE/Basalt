namespace BasaltBE.Nbt.Types;

/// <summary>Sets resource limits for one NBT read.</summary>
public readonly struct NbtReadLimits {
    public readonly int MaximumDepth;
    public readonly int MaximumTags;
    public readonly int MaximumListLength;
    public readonly int MaximumArrayElements;

    public NbtReadLimits(int maximumDepth, int maximumTags, int maximumListLength, int maximumArrayElements) {
        if (maximumDepth < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumDepth));
        if (maximumTags < 1)
            throw new ArgumentOutOfRangeException(nameof(maximumTags));
        if (maximumListLength < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumListLength));
        if (maximumArrayElements < 0)
            throw new ArgumentOutOfRangeException(nameof(maximumArrayElements));

        MaximumDepth = maximumDepth;
        MaximumTags = maximumTags;
        MaximumListLength = maximumListLength;
        MaximumArrayElements = maximumArrayElements;
    }
}

internal struct NbtReadContext(NbtReadLimits? limits) {
    internal readonly NbtReadLimits? Limits = limits;
    internal int Depth;
    internal int Tags;
    internal int ArrayElements;

    internal void EnterTag() {
        if (Limits is not { } limits)
            return;
        if (Depth >= limits.MaximumDepth)
            throw new InvalidDataException("NBT nesting depth exceeds the read limit.");
        if (++Tags > limits.MaximumTags)
            throw new InvalidDataException("NBT tag count exceeds the read limit.");
        Depth++;
    }

    internal void ExitTag() => Depth--;

    internal void CheckListLength(int count) {
        if (Limits is { } limits) {
            if (count > limits.MaximumListLength)
                throw new InvalidDataException("NBT list length exceeds the read limit.");
            if (count > limits.MaximumTags - Tags)
                throw new InvalidDataException("NBT list exceeds the remaining tag budget.");
        }
    }

    internal void CheckArrayLength(int count) {
        if (Limits is { } limits) {
            if (count > limits.MaximumArrayElements - ArrayElements)
                throw new InvalidDataException("NBT array elements exceed the read limit.");
            ArrayElements += count;
        }
    }
}
