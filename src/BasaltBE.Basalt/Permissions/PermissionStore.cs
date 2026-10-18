using System.Globalization;
using System.Text.Json;
using BasaltBE.Basalt.Entities;

namespace BasaltBE.Basalt.Permissions;

public sealed class PermissionStore {
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly Lock sync = new();
    private readonly Lock fileSync = new();
    private Dictionary<string, PlayerPermissions> entries = new(StringComparer.Ordinal);
    private Dictionary<string, string> playerNames = new(StringComparer.Ordinal);
    private Dictionary<string, string> playerXuids = new(StringComparer.OrdinalIgnoreCase);
    private long savedRevision = -1;
    private string? savedPath;
    internal long Revision;

    public PlayerPermissions Get(string xuid, string? username = null) {
        if (!ulong.TryParse(xuid, NumberStyles.None, CultureInfo.InvariantCulture, out ulong value) || value == 0)
            throw new ArgumentException("A valid XUID is required.", nameof(xuid));
        lock (sync) {
            if (username is not null && (!playerNames.TryGetValue(xuid, out string? previous) || previous != username)) {
                ArgumentException.ThrowIfNullOrWhiteSpace(username);
                if (previous is not null && playerXuids.GetValueOrDefault(previous) == xuid)
                    playerXuids.Remove(previous);
                if (playerXuids.TryGetValue(username, out string? formerXuid) && formerXuid != xuid)
                    playerNames.Remove(formerXuid);
                playerNames[xuid] = username;
                playerXuids[username] = xuid;
                Interlocked.Increment(ref Revision);
            }
            if (!entries.TryGetValue(xuid, out PlayerPermissions? permissions)) {
                permissions = new PlayerPermissions(this);
                entries.Add(xuid, permissions);
            }
            return permissions;
        }
    }

    public PlayerPermissions? GetByName(string username) {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        lock (sync)
            return playerXuids.TryGetValue(username, out string? xuid) ? Get(xuid) : null;
    }

    public void Load(string path) {
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) {
            Save(fullPath);
            return;
        }

        using Lock.Scope fileLock = fileSync.EnterScope();
        using FileStream stream = File.OpenRead(fullPath);
        Dictionary<string, string[]> data = JsonSerializer.Deserialize<Dictionary<string, string[]>>(stream)
            ?? throw new InvalidDataException("Permission data must be an object keyed by XUID.");
        Dictionary<string, PlayerPermissions> loaded = new(StringComparer.Ordinal);
        foreach ((string xuid, string[] values) in data) {
            if (!ulong.TryParse(xuid, NumberStyles.None, CultureInfo.InvariantCulture, out ulong value) || value == 0 ||
                values is null || values.Any(string.IsNullOrWhiteSpace))
                throw new InvalidDataException($"Invalid permission entry for XUID '{xuid}'.");
            PlayerPermissions permissions = new(this);
            foreach (string permission in values)
                permissions.Add(permission);
            loaded.Add(xuid, permissions);
        }
        string namesPath = Path.ChangeExtension(fullPath, ".players.json");
        Dictionary<string, string> names = new(StringComparer.Ordinal);
        Dictionary<string, string> xuids = new(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(namesPath)) {
            using FileStream namesStream = File.OpenRead(namesPath);
            names = JsonSerializer.Deserialize<Dictionary<string, string>>(namesStream)
                ?? throw new InvalidDataException("Player names must be an object keyed by XUID.");
            foreach ((string xuid, string username) in names) {
                if (!ulong.TryParse(xuid, NumberStyles.None, CultureInfo.InvariantCulture, out ulong value) || value == 0 ||
                    string.IsNullOrWhiteSpace(username) || !xuids.TryAdd(username, xuid))
                    throw new InvalidDataException($"Invalid player name entry for XUID '{xuid}'.");
            }
        }
        lock (sync) {
            entries = loaded;
            playerNames = names;
            playerXuids = xuids;
        }
        savedRevision = Interlocked.Increment(ref Revision);
        savedPath = fullPath;
    }

    public void Save(string path) {
        string fullPath = Path.GetFullPath(path);
        using Lock.Scope fileLock = fileSync.EnterScope();
        long revision = Volatile.Read(ref Revision);
        if (savedRevision == revision && savedPath == fullPath && File.Exists(fullPath))
            return;

        Dictionary<string, string[]> snapshot = new(StringComparer.Ordinal);
        Dictionary<string, string> names;
        lock (sync) {
            names = new(playerNames, StringComparer.Ordinal);
            foreach ((string xuid, PlayerPermissions permissions) in entries) {
                string[] values = permissions.Snapshot;
                if (values.Length > 0)
                    snapshot.Add(xuid, values);
            }
        }
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        string temporaryPath = fullPath + ".tmp";
        using (FileStream stream = new(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
            JsonSerializer.Serialize(stream, snapshot, JsonOptions);
            stream.Flush(flushToDisk: true);
        }
        File.Move(temporaryPath, fullPath, overwrite: true);
        string namesPath = Path.ChangeExtension(fullPath, ".players.json");
        string temporaryNamesPath = namesPath + ".tmp";
        using (FileStream stream = new(temporaryNamesPath, FileMode.Create, FileAccess.Write, FileShare.None)) {
            JsonSerializer.Serialize(stream, names, JsonOptions);
            stream.Flush(flushToDisk: true);
        }
        File.Move(temporaryNamesPath, namesPath, overwrite: true);
        savedRevision = revision;
        savedPath = fullPath;
    }
}
