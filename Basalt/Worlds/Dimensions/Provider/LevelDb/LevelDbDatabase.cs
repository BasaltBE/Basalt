using LevelDB.Core.Api;
using LevelDB.Core.Util;
using LevelDB.MCPE;
using LevelDB.Platform;
using CoreWriteBatch = LevelDB.Core.Db.WriteBatch;

namespace Basalt.Core.Worlds.Dimensions.Provider;

internal sealed class LevelDbDatabase : IDisposable {
    private static readonly ReadOptions ReadOptions = new();
    private static readonly WriteOptions WriteOptions = new();
    private readonly IDB _database;

    public LevelDbDatabase(string path) {
        Options options = McpeOptions.Create(new Options { CreateIfMissing = true });
        Status status = DB.Open(options, path, out IDB? database);
        status.ThrowIfNotOk();
        _database = database ?? throw new InvalidOperationException($"Could not open LevelDB database at '{path}'.");
    }

    public byte[]? Get(byte[] key) {
        Status status = _database.Get(ReadOptions, new Slice(key), out Slice value);
        if (status.IsNotFound) {
            return null;
        }

        status.ThrowIfNotOk();
        return value.ToArray();
    }

    public void Put(byte[] key, byte[] value) {
        _database.Put(WriteOptions, new Slice(key), new Slice(value)).ThrowIfNotOk();
    }

    public void Delete(byte[] key) {
        _database.Delete(WriteOptions, new Slice(key)).ThrowIfNotOk();
    }

    public void Write(LevelDbWriteBatch batch) {
        _database.Write(WriteOptions, batch.Batch).ThrowIfNotOk();
    }

    public LevelDbIterator CreateIterator() {
        return new LevelDbIterator(_database.NewIterator(ReadOptions));
    }

    public void Dispose() {
        _database.Dispose();
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
