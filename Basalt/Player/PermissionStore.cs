using System.Text.Json;
using System.Text.Json.Serialization;

namespace Basalt.Core.Player;

/// <summary>
/// Holds player permissions
/// </summary>
public sealed class PermissionStore
{
    private const string DefaultFileName = "permissions.json";

    private readonly string _filePath;
    private readonly Dictionary<string, PermissionEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();

    public PermissionStore(string filePath)
    {
        _filePath = filePath;
        Load();
    }

    public PermissionStore() : this(DefaultFileName) { }

    public PermissionEntry? Get(string xuid)
    {
        if (string.IsNullOrWhiteSpace(xuid))
        {
            return null;
        }

        lock (_lock)
        {
            return _entries.GetValueOrDefault(xuid);
        }
    }

    public void Save(string xuid, string username, bool isOperator, IEnumerable<string> permissions)
    {
        if (string.IsNullOrWhiteSpace(xuid))
        {
            return;
        }

        lock (_lock)
        {
            _entries[xuid] = new PermissionEntry
            {
                Xuid = xuid,
                Username = username,
                IsOperator = isOperator,
                Permissions = [.. permissions]
            };
            Persist();
        }
    }

    public void Remove(string xuid)
    {
        if (string.IsNullOrWhiteSpace(xuid))
        {
            return;
        }

        lock (_lock)
        {
            if (_entries.Remove(xuid))
            {
                Persist();
            }
        }
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            List<PermissionEntry>? entries = JsonSerializer.Deserialize(json, PermissionStoreContext.Default.ListPermissionEntry);
            if (entries is null)
            {
                return;
            }

            foreach (PermissionEntry entry in entries)
            {
                if (!string.IsNullOrWhiteSpace(entry.Xuid))
                {
                    _entries[entry.Xuid] = entry;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to load permissions file: {ex.Message}");
        }
    }

    private void Persist()
    {
        try
        {
            List<PermissionEntry> entries = [.. _entries.Values];
            string json = JsonSerializer.Serialize(entries, PermissionStoreContext.Default.ListPermissionEntry);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to save permissions file: {ex.Message}");
        }
    }
}

public sealed class PermissionEntry
{
    [JsonPropertyName("xuid")]
    public string Xuid { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("isOperator")]
    public bool IsOperator { get; set; }

    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; } = [];
}

[JsonSerializable(typeof(List<PermissionEntry>))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class PermissionStoreContext : JsonSerializerContext;
