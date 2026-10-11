using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using BasaltBE.Basalt.Profiling;

namespace BasaltBE.Basalt.Network.ResourcePacks;

internal sealed class ResourcePack {
    public Guid Id;
    public string Name = string.Empty;
    public string Description = string.Empty;
    public string Version = string.Empty;
    public string ResourceName = string.Empty;
    public byte[] Archive = [];
    public int ArchiveLength;
    public byte[] Hash = [];

    public uint GetChunkCount(uint chunkSize) {
        ArgumentOutOfRangeException.ThrowIfZero(chunkSize);
        return checked((uint)((ArchiveLength + (long)chunkSize - 1) / chunkSize));
    }

    public ReadOnlyMemory<byte> GetChunk(uint chunk, uint chunkSize) {
        using ProfilerZone zone = Profiler.BeginZone("ResourcePacks.GetChunk");
        uint count = GetChunkCount(chunkSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(chunk, count);
        int offset = checked((int)((ulong)chunk * chunkSize));
        int length = Math.Min((int)chunkSize, ArchiveLength - offset);
        return Archive.AsMemory(offset, length);
    }

    internal static ResourcePack Load(string directory) {
        using ProfilerZone zone = Profiler.BeginZone("ResourcePacks.LoadPack");
        string manifestPath = Path.Combine(directory, "manifest.json");
        using FileStream manifestStream = File.OpenRead(manifestPath);
        ResourcePackManifest manifest = JsonSerializer.Deserialize<ResourcePackManifest>(manifestStream, JsonOptions)
            ?? throw new InvalidDataException($"Resource pack manifest is empty: {manifestPath}.");
        ResourcePackManifestHeader header = manifest.Header
            ?? throw new InvalidDataException($"Resource pack manifest has no header: {manifestPath}.");

        if (!Guid.TryParse(header.Uuid, out Guid id) || id == Guid.Empty)
            throw new InvalidDataException($"Resource pack has an invalid header UUID: {manifestPath}.");
        if (string.IsNullOrWhiteSpace(header.Name) || header.Version is null || header.Version.Length != 3 ||
            header.Version.Any(version => version < 0))
            throw new InvalidDataException($"Resource pack manifest has an invalid name or version: {manifestPath}.");

        using MemoryStream archiveStream = new();
        using (Profiler.BeginZone("ResourcePacks.Archive"))
            ZipFile.CreateFromDirectory(directory, archiveStream, CompressionLevel.Fastest, includeBaseDirectory: false);

        if (!archiveStream.TryGetBuffer(out ArraySegment<byte> archiveBuffer) || archiveBuffer.Array is null)
            throw new InvalidOperationException("Resource pack archive buffer is unavailable.");
        int archiveLength = checked((int)archiveStream.Length);
        byte[] archive = archiveBuffer.Array;
        byte[] hash;
        using (Profiler.BeginZone("ResourcePacks.Hash"))
            hash = SHA256.HashData(archive.AsSpan(archiveBuffer.Offset, archiveLength));

        return new ResourcePack {
            Id = id,
            Name = header.Name,
            Description = header.Description,
            Version = string.Join('.', header.Version),
            ResourceName = id.ToString(),
            Archive = archive,
            ArchiveLength = archiveLength,
            Hash = hash
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new() {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true
    };
}
