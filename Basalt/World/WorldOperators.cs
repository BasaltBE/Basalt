using System.Text.Json;

namespace Basalt.World;

public sealed class WorldOperators
{
    private readonly string _path;
    private readonly HashSet<string> _operators = new(StringComparer.Ordinal);

    public WorldOperators(string path)
    {
        _path = path;
    }

    public bool IsOperator(string xuid)
    {
        return !string.IsNullOrWhiteSpace(xuid) && _operators.Contains(xuid);
    }

    public void AddOperator(string xuid)
    {
        if (string.IsNullOrWhiteSpace(xuid))
        {
            return;
        }

        if (_operators.Add(xuid))
        {
            Save();
        }
    }

    public void RemoveOperator(string xuid)
    {
        if (string.IsNullOrWhiteSpace(xuid))
        {
            return;
        }

        if (_operators.Remove(xuid))
        {
            Save();
        }
    }

    public void Load()
    {
        _operators.Clear();

        if (!File.Exists(_path))
        {
            return;
        }

        string json = File.ReadAllText(_path);
        string[]? xuids = JsonSerializer.Deserialize<string[]>(json);
        if (xuids is null)
        {
            return;
        }

        for (int i = 0; i < xuids.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(xuids[i]))
            {
                _operators.Add(xuids[i]);
            }
        }
    }

    public void Save()
    {
        string? directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(_operators.ToArray()));
    }
}
