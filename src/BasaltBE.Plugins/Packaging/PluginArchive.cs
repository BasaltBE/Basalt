using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace BasaltBE.Plugins.Packaging;

public sealed class PluginArchive {
    private readonly string path;
    public readonly PluginManifest Manifest;
    public readonly PackageDescriptor Descriptor;
    public readonly string[] Files;

    public PluginArchive(string path) {
        this.path = Path.GetFullPath(path);
        using ZipArchive archive = ZipFile.OpenRead(this.path);
        ValidateEntries(archive);

        ZipArchiveEntry manifestEntry = archive.GetEntry("plugin.json") ?? throw new InvalidDataException("Plugin manifest is missing.");
        ZipArchiveEntry descriptorEntry = archive.GetEntry("package.json") ?? throw new InvalidDataException("Package descriptor is missing.");
        if (manifestEntry.Length > 65536 || descriptorEntry.Length > 65536)
            throw new InvalidDataException("Plugin metadata exceeds 64 KiB.");

        using (Stream stream = manifestEntry.Open())
            Manifest = PluginManifest.Read(stream);
        using (Stream stream = descriptorEntry.Open())
            Descriptor = JsonSerializer.Deserialize<PackageDescriptor>(stream, PluginManifest.Json)
                ?? throw new InvalidDataException("Package descriptor is empty.");

        if (Descriptor.FormatVersion != 1 || Descriptor.TargetFramework != "net10.0")
            throw new InvalidDataException("Unsupported plugin package version or target framework.");
        if (Descriptor.EntryAssembly != Manifest.Name + ".dll" || archive.GetEntry(Descriptor.EntryAssembly) is null ||
            archive.GetEntry(Manifest.Name + ".deps.json") is null || string.IsNullOrWhiteSpace(Descriptor.EntryType))
            throw new InvalidDataException("Plugin entry point or dependency graph is missing.");
        if (Descriptor.HostAssemblies is null || Descriptor.HostAssemblies.Any(name => string.IsNullOrWhiteSpace(name) || Path.GetFileName(name) != name))
            throw new InvalidDataException("Invalid host assembly list.");

        Files = archive.Entries.Select(entry => entry.FullName).Order(StringComparer.Ordinal).ToArray();
    }

    public string Extract(string cacheDirectory) {
        using FileStream input = File.OpenRead(path);
        string hash = Convert.ToHexStringLower(SHA256.HashData(input));
        string cache = Path.Combine(Path.GetFullPath(cacheDirectory), hash);
        if (Directory.Exists(cache)) return cache;

        string staging = cache + "." + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(staging);
        try {
            input.Position = 0;
            using ZipArchive archive = new(input, ZipArchiveMode.Read, true);
            ValidateEntries(archive);
            foreach (ZipArchiveEntry entry in archive.Entries) {
                string destination = Path.GetFullPath(Path.Combine(staging, entry.FullName));
                if (!destination.StartsWith(staging + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                    throw new InvalidDataException("Archive path escapes its directory.");

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                using Stream source = entry.Open();
                using FileStream target = new(destination, FileMode.CreateNew);
                source.CopyTo(target);
            }
            Directory.Move(staging, cache);
            return cache;
        } finally {
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
        }
    }

    public static void Write(string directory, string destination) {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);
        string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try {
            using (ZipArchive archive = ZipFile.Open(temporary, ZipArchiveMode.Create)) {
                IEnumerable<(string Path, string Name)> files = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
                    .Select(file => (Path: file, Name: Path.GetRelativePath(directory, file).Replace('\\', '/')))
                    .OrderBy(file => file.Name, StringComparer.Ordinal);

                foreach ((string file, string name) in files) {
                    ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Optimal);
                    entry.LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
                    using Stream source = File.OpenRead(file);
                    using Stream target = entry.Open();
                    source.CopyTo(target);
                }
            }

            _ = new PluginArchive(temporary);
            File.Move(temporary, destination, true);
        } finally {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static void ValidateEntries(ZipArchive archive) {
        if (archive.Entries.Count > 10000) throw new InvalidDataException("Plugin archive has too many entries.");

        HashSet<string> files = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);
        long totalSize = 0;

        foreach (ZipArchiveEntry entry in archive.Entries) {
            string name = entry.FullName;
            if (name.StartsWith('/') || name.Contains('\\') || name.Contains(':') ||
                name.Any(character => character < 32 || "<>\"|?*".Contains(character)))
                throw new InvalidDataException($"Invalid archive path: {name}.");

            foreach (string part in name.Split('/')) {
                if (part is "" or "." or ".." || part.EndsWith('.') || part.EndsWith(' '))
                    throw new InvalidDataException($"Invalid archive path: {name}.");
            }

            if (!files.Add(name)) throw new InvalidDataException($"Duplicate archive path: {name}.");
            if (directories.Contains(name)) throw new InvalidDataException($"Archive file conflicts with directory: {name}.");

            for (int slash = name.IndexOf('/'); slash >= 0; slash = name.IndexOf('/', slash + 1)) {
                string parent = name[..slash];
                if (files.Contains(parent)) throw new InvalidDataException($"Archive file conflicts with directory: {parent}.");

                directories.Add(parent);
            }

            if (entry.Length > 512L * 1024 * 1024 - totalSize) throw new InvalidDataException("Plugin archive exceeds 512 MiB.");

            totalSize += entry.Length;
        }
    }
}
