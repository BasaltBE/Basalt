namespace Basalt.Core.Resources;

using System.IO.Compression;
using System.Text.Json;

/// <summary>
/// Loads and manages resource packs from the configured folder.
/// Each subfolder with a valid manifest.json is treated as a resource pack.
/// </summary>
public sealed class ResourcePackManager
{
    private const uint DefaultChunkSize = 1048576; // 1 MB

    private readonly List<ResourcePack> _packs = [];

    public IReadOnlyList<ResourcePack> Packs => _packs;
    public uint ChunkSize { get; set; } = DefaultChunkSize;

    public ResourcePack? GetByUuid(Guid uuid)
    {
        for (int i = 0; i < _packs.Count; i++)
        {
            if (_packs[i].Uuid == uuid)
            {
                return _packs[i];
            }
        }

        return null;
    }

    public ResourcePack? GetByUuid(string uuidString)
    {
        // Client sends UUID with a version suffix like "uuid_version", strip it.
        int underscoreIndex = uuidString.IndexOf('_');
        ReadOnlySpan<char> rawUuid = underscoreIndex >= 0
            ? uuidString.AsSpan(0, underscoreIndex)
            : uuidString.AsSpan();

        if (!Guid.TryParse(rawUuid, out Guid uuid))
        {
            return null;
        }

        return GetByUuid(uuid);
    }

    public void Load(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            folder = "resource_packs";
        }

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        foreach (string packDir in Directory.EnumerateDirectories(folder))
        {
            string manifestPath = Path.Combine(packDir, "manifest.json");
            if (!File.Exists(manifestPath))
            {
                continue;
            }

            try
            {
                ResourcePack pack = LoadPack(packDir, manifestPath);
                _packs.Add(pack);
            }
            catch (Exception ex)
            {
                string dirName = Path.GetFileName(packDir);
                Logger.Warn($"Resource pack '{dirName}' failed to load: {ex.Message}");
            }
        }

        if (_packs.Count == 0)
        {
            Logger.Info($"No resource packs found in '{folder}'.");
        }
        else
        {
            string names = string.Join(", ", _packs.Select(p => $"{p.Name} v{p.VersionString}"));
            Logger.Info($"Loaded {_packs.Count} resource pack(s): {names}");
        }
    }

    private static ResourcePack LoadPack(string packDir, string manifestPath)
    {
        string manifestJson = File.ReadAllText(manifestPath);
        using JsonDocument doc = JsonDocument.Parse(manifestJson);
        JsonElement root = doc.RootElement;

        if (!root.TryGetProperty("header", out JsonElement header))
        {
            throw new InvalidOperationException("Manifest missing 'header'.");
        }

        string uuid = header.TryGetProperty("uuid", out JsonElement uuidEl)
            ? uuidEl.GetString() ?? throw new InvalidOperationException("Manifest header.uuid is null.")
            : throw new InvalidOperationException("Manifest missing 'header.uuid'.");

        if (!Guid.TryParse(uuid, out Guid parsedUuid))
        {
            throw new InvalidOperationException($"Invalid UUID '{uuid}' in manifest.");
        }

        string name = header.TryGetProperty("name", out JsonElement nameEl)
            ? nameEl.GetString() ?? "(unnamed)"
            : "(unnamed)";

        string description = header.TryGetProperty("description", out JsonElement descEl)
            ? descEl.GetString() ?? ""
            : "";

        int[] version = ParseVersion(header);

        byte[] zipData = CompressToZip(packDir);

        return ResourcePack.Create(Path.GetFileName(packDir), parsedUuid, name, description, version, zipData);
    }

    private static int[] ParseVersion(JsonElement header)
    {
        if (!header.TryGetProperty("version", out JsonElement versionEl) || versionEl.ValueKind != JsonValueKind.Array)
        {
            return [0, 0, 0];
        }

        int[] version = new int[3];
        int index = 0;
        foreach (JsonElement item in versionEl.EnumerateArray())
        {
            if (index >= 3) break;
            version[index] = item.TryGetInt32(out int val) ? val : 0;
            index++;
        }

        return version;
    }

    private static byte[] CompressToZip(string directory)
    {
        using MemoryStream ms = new();
        using (ZipArchive archive = new(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddDirectoryToZip(archive, directory, "");
        }

        return ms.ToArray();
    }

    private static void AddDirectoryToZip(ZipArchive archive, string sourceDir, string entryPrefix)
    {
        foreach (string filePath in Directory.EnumerateFiles(sourceDir))
        {
            string entryName = string.IsNullOrEmpty(entryPrefix)
                ? Path.GetFileName(filePath)
                : $"{entryPrefix}/{Path.GetFileName(filePath)}";

            ZipArchiveEntry entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using Stream entryStream = entry.Open();
            using FileStream fileStream = File.OpenRead(filePath);
            fileStream.CopyTo(entryStream);
        }

        foreach (string subDir in Directory.EnumerateDirectories(sourceDir))
        {
            string dirName = Path.GetFileName(subDir);
            string newPrefix = string.IsNullOrEmpty(entryPrefix)
                ? dirName
                : $"{entryPrefix}/{dirName}";

            AddDirectoryToZip(archive, subDir, newPrefix);
        }
    }
}
