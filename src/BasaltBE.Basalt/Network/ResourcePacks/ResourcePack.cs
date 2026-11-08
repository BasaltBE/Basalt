using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
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

        FileInfo[] files = new DirectoryInfo(directory).GetFiles("*", SearchOption.AllDirectories);
        Array.Sort(files, static (left, right) => StringComparer.Ordinal.Compare(left.FullName, right.FullName));
        using IncrementalHash fingerprint = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (FileInfo file in files) {
            fingerprint.AppendData(Encoding.UTF8.GetBytes(file.FullName));
            fingerprint.AppendData(BitConverter.GetBytes(file.Length));
            fingerprint.AppendData(BitConverter.GetBytes(file.LastWriteTimeUtc.Ticks));
        }
        string signature = Convert.ToHexString(fingerprint.GetHashAndReset());
        string cache = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(directory))!, ".cache");
        string archivePath = Path.Combine(cache, id + ".zip");
        string signaturePath = Path.Combine(cache, id + ".stamp");
        byte[]? archive = null;
        byte[]? hash = null;
        try {
            if (File.Exists(signaturePath) && File.Exists(archivePath)) {
                string[] saved = File.ReadAllLines(signaturePath);
                if (saved.Length == 2 && saved[0] == signature) {
                    byte[] cached = File.ReadAllBytes(archivePath);
                    byte[] cachedHash = SHA256.HashData(cached);
                    if (Convert.ToHexString(cachedHash) == saved[1]) {
                        archive = cached;
                        hash = cachedHash;
                    }
                }
            }
        } catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }

        int archiveLength;
        if (archive is null) {
            using MemoryStream archiveStream = new();
            using (Profiler.BeginZone("ResourcePacks.Archive")) {
                using ZipArchive zip = new(archiveStream, ZipArchiveMode.Create, leaveOpen: true);
                foreach (FileInfo file in files) {
                    string name = Path.GetRelativePath(directory, file.FullName).Replace('\\', '/');
                    zip.CreateEntryFromFile(file.FullName, name, CompressionLevel.Fastest);
                }
            }
            if (!archiveStream.TryGetBuffer(out ArraySegment<byte> archiveBuffer) || archiveBuffer.Array is null)
                throw new InvalidOperationException("Resource pack archive buffer is unavailable.");
            archiveLength = checked((int)archiveStream.Length);
            archive = archiveBuffer.Array;
            hash = SHA256.HashData(archive.AsSpan(0, archiveLength));

            string temporary = archivePath + "." + Guid.NewGuid().ToString("N");
            try {
                Directory.CreateDirectory(cache);
                using (FileStream output = File.Create(temporary)) output.Write(archive.AsSpan(0, archiveLength));
                File.Move(temporary, archivePath, overwrite: true);
                File.WriteAllLines(signaturePath, [signature, Convert.ToHexString(hash)]);
            } catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
            } finally {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        } else {
            archiveLength = archive.Length;
        }

        return new ResourcePack {
            Id = id,
            Name = header.Name,
            Description = header.Description,
            Version = string.Join('.', header.Version),
            ResourceName = id.ToString(),
            Archive = archive,
            ArchiveLength = archiveLength,
            Hash = hash!
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new() {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true
    };
}
