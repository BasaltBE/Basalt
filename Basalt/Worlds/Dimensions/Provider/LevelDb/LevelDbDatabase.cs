using LevelDB.Core.Api;
using LevelDB.Core.Util;
using LevelDB.MCPE;
using LevelDB.Platform;
using CoreWriteBatch = LevelDB.Core.Db.WriteBatch;
using System.Diagnostics;

namespace Basalt.Core.Worlds.Dimensions.Provider;

internal sealed class LevelDbDatabase : IDisposable {
    private readonly ReadOptions _readOptions = new();
    private readonly WriteOptions _writeOptions = new();
    private readonly IDB _database;
    private readonly Lock _writeLock = new();
    private long _readTicks;
    private long _readCount;
    private long _writeTicks;
    private long _writeCount;

    public long ReadCount => Interlocked.Read(ref _readCount);
    public long WriteCount => Interlocked.Read(ref _writeCount);
    public double AverageReadMilliseconds => GetAverageMilliseconds(
        Interlocked.Read(ref _readTicks),
        Interlocked.Read(ref _readCount));
    public double AverageWriteMilliseconds => GetAverageMilliseconds(
        Interlocked.Read(ref _writeTicks),
        Interlocked.Read(ref _writeCount));

    public LevelDbDatabase(string path) {
        Options options = McpeOptions.Create(new Options { CreateIfMissing = true });
        Status status = DB.Open(options, path, out IDB? database);
        status.ThrowIfNotOk();
        _database = database ?? throw new InvalidOperationException($"Could not open LevelDB database at '{path}'.");
    }

    public byte[]? Get(byte[] key) {
        long start = Stopwatch.GetTimestamp();
        try {
            Status status = _database.Get(_readOptions, new Slice(key), out Slice value);
            if (status.IsNotFound) {
                return null;
            }

            status.ThrowIfNotOk();
            return value.ToArray();
        }
        finally {
            Interlocked.Add(ref _readTicks, Stopwatch.GetTimestamp() - start);
            Interlocked.Increment(ref _readCount);
        }
    }

    public void Put(byte[] key, byte[] value) {
        lock (_writeLock) {
            long start = Stopwatch.GetTimestamp();
            try {
                _database.Put(_writeOptions, new Slice(key), new Slice(value)).ThrowIfNotOk();
            }
            finally {
                Interlocked.Add(ref _writeTicks, Stopwatch.GetTimestamp() - start);
                Interlocked.Increment(ref _writeCount);
            }
        }
    }

    public void Delete(byte[] key) {
        lock (_writeLock) {
            long start = Stopwatch.GetTimestamp();
            try {
                _database.Delete(_writeOptions, new Slice(key)).ThrowIfNotOk();
            }
            finally {
                Interlocked.Add(ref _writeTicks, Stopwatch.GetTimestamp() - start);
                Interlocked.Increment(ref _writeCount);
            }
        }
    }

    public void Write(LevelDbWriteBatch batch) {
        lock (_writeLock) {
            long start = Stopwatch.GetTimestamp();
            try {
                _database.Write(_writeOptions, batch.Batch).ThrowIfNotOk();
            }
            finally {
                Interlocked.Add(ref _writeTicks, Stopwatch.GetTimestamp() - start);
                Interlocked.Increment(ref _writeCount);
            }
        }
    }

    public LevelDbIterator CreateIterator() {
        return new LevelDbIterator(_database.NewIterator(_readOptions));
    }

    public void Dispose() {
        lock (_writeLock) {
            _database.Dispose();
        }
    }

    private static double GetAverageMilliseconds(long ticks, long count) {
        return count == 0 ? 0 : ticks * 1000.0 / Stopwatch.Frequency / count;
    }
}

internal sealed class LevelDbWriteBatch {
    internal CoreWriteBatch Batch { get; } = new();

    public void Put(byte[] key, byte[] value) {
        Batch.Put(new Slice(key), new Slice(value));
    }

    public void Delete(byte[] key) {
        Batch.Delete(new Slice(key));
    }
}

internal sealed class LevelDbIterator : IDisposable {
    private readonly IIterator _iterator;

    public LevelDbIterator(IIterator iterator) {
        _iterator = iterator;
    }

    public void Seek(byte[] key) {
        _iterator.Seek(new Slice(key));
    }

    public bool Valid() {
        return _iterator.Valid;
    }

    public ReadOnlySpan<byte> Key() {
        return _iterator.Key.Span;
    }

    public void Next() {
        _iterator.Next();
    }

    public void Dispose() {
        _iterator.Dispose();
    }
}
