namespace Basalt.Core.Player;

using System.Text.Json;
using System.Text.Encodings.Web;

public sealed class BanStore {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly string _path;
    private readonly List<BanEntry> _entries;

    public BanStore(string path) {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        _entries = File.Exists(path)
            ? JsonSerializer.Deserialize<List<BanEntry>>(File.ReadAllText(path)) ?? []
            : [];
    }

    public bool IsBanned(string identifier, out BanEntry? entry) {
        lock (_entries) {
            bool changed = false;
            for (int i = _entries.Count - 1; i >= 0; i--) {
                BanEntry current = _entries[i];
                if (current.Until != 0 && current.Until <= DateTimeOffset.UtcNow.ToUnixTimeSeconds()) {
                    _entries.RemoveAt(i);
                    changed = true;
                    continue;
                }

                if (Matches(current, identifier)) {
                    entry = current;
                    if (changed) {
                        Save();
                    }
                    return true;
                }
            }

            if (changed) {
                Save();
            }
        }

        entry = null;
        return false;
    }

    public bool IsBanned(string xuid, string username, out BanEntry? entry) {
        if (IsBanned(xuid, out entry)) {
            return true;
        }

        return IsBanned(username, out entry);
    }

    public void Ban(BanEntry entry) {
        lock (_entries) {
            _entries.RemoveAll(current => Matches(current, entry.Identifier)
                || (!string.IsNullOrEmpty(entry.Xuid) && string.Equals(current.Xuid, entry.Xuid, StringComparison.Ordinal))
                || (!string.IsNullOrEmpty(entry.Username) && string.Equals(current.Username, entry.Username, StringComparison.OrdinalIgnoreCase)));
            _entries.Add(entry);
            Save();
        }
    }

    public bool Remove(string identifier) {
        lock (_entries) {
            int removed = _entries.RemoveAll(entry => Matches(entry, identifier));
            if (removed == 0) {
                return false;
            }

            Save();
            return true;
        }
    }

    private void Save() {
        string temporaryPath = _path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(_entries, JsonOptions));
        File.Move(temporaryPath, _path, true);
    }

    private static bool Matches(BanEntry entry, string identifier) {
        return string.Equals(entry.Identifier, identifier, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrEmpty(entry.Xuid) && string.Equals(entry.Xuid, identifier, StringComparison.Ordinal))
            || (!string.IsNullOrEmpty(entry.Username) && string.Equals(entry.Username, identifier, StringComparison.OrdinalIgnoreCase));
    }
}
