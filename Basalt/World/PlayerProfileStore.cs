using Basalt.Protocol.Enums;
using Basalt.Protocol.Nbt;
using Basalt.World.Dimension.Provider;

namespace Basalt.World;

public sealed class PlayerProfileStore
{
    private readonly WorldProvider _provider;
    private readonly Dictionary<string, string> _usernameToXuid = new(StringComparer.OrdinalIgnoreCase);

    public PlayerProfileStore(WorldProvider provider)
    {
        _provider = provider;
    }

    public void RebuildIndex()
    {
        _usernameToXuid.Clear();

        foreach (string xuid in _provider.ListPlayerXuids())
        {
            CompoundTag? data = _provider.LoadPlayerData(xuid);
            string? username = data?.Get<StringTag>("username")?.Value;
            if (!string.IsNullOrWhiteSpace(username))
            {
                _usernameToXuid[username] = xuid;
            }
        }
    }

    public void UpdateIndex(string username, string xuid)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(xuid))
        {
            return;
        }

        _usernameToXuid[username] = xuid;
    }

    public bool TryGetXuid(string username, out string xuid)
    {
        return _usernameToXuid.TryGetValue(username, out xuid!);
    }

    public CompoundTag? LoadProfile(string xuid)
    {
        return _provider.LoadPlayerData(xuid);
    }

    public void SaveProfile(string xuid, CompoundTag data)
    {
        _provider.SavePlayerData(xuid, data);
        string? username = data.Get<StringTag>("username")?.Value;
        if (!string.IsNullOrWhiteSpace(username))
        {
            UpdateIndex(username, xuid);
        }
    }

    public bool TryUpdateGamemode(string username, Gamemode gamemode)
    {
        if (!TryGetXuid(username, out string xuid))
        {
            return false;
        }

        CompoundTag? data = LoadProfile(xuid);
        if (data is null)
        {
            return false;
        }

        data.Set("gamemode", new IntTag { Value = (int)gamemode });
        SaveProfile(xuid, data);
        return true;
    }

    public bool TryUpdateOperator(string username, bool isOperator)
    {
        if (!TryGetXuid(username, out string xuid))
        {
            return false;
        }

        CompoundTag? data = LoadProfile(xuid);
        if (data is null)
        {
            return false;
        }

        data.Set("isOp", new ByteTag { Value = isOperator ? (sbyte)1 : (sbyte)0 });
        SaveProfile(xuid, data);
        return true;
    }
}
