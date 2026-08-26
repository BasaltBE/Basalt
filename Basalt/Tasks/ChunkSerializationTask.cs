namespace Basalt.Core.Tasks;

using Basalt.Core.Worlds.Dimensions.Chunk;
using ChunkColumn = Basalt.Core.Worlds.Dimensions.Chunk.Chunk;

public sealed class ChunkSerializationTask : ServerTask {
    private readonly ChunkColumn _chunk;
    private readonly Action<ChunkColumn, byte[]?, Exception?> _onComplete;

    public byte[]? Result { get; private set; }
    public Exception? Error { get; private set; }

    public ChunkSerializationTask(
        ChunkColumn chunk,
        Action<ChunkColumn, byte[]?, Exception?> onComplete) {
        _chunk = chunk;
        _onComplete = onComplete;
    }

    public override void Execute() {
        try {
            Result = ChunkColumn.Serialize(_chunk);
        }
        catch (Exception exception) {
            Error = exception;
        }
    }

    public override void Complete() {
        _onComplete(_chunk, Result, Error);
    }
}
