namespace Basalt.Core.Player;

using Basalt.Protocol.Packets;

public sealed class PlayerPermissions
{
    private readonly HashSet<string> _permissions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Player _player;

    internal PlayerPermissions(Player player)
    {
        _player = player;
    }

    public void Add(string permission, bool syncClient = true)
    {
        _permissions.Add(permission);
        if (syncClient)
        {
            Sync();
        }
    }

    public void Remove(string permission, bool syncClient = true)
    {
        _permissions.Remove(permission);
        if (syncClient)
        {
            Sync();
        }
    }

    public bool Has(string permission)
    {
        return _permissions.Contains(permission);
    }

    public void Sync()
    {
        if (_player.Connection is null || _player.Network is null)
        {
            return;
        }

        _player.Network.SendPacket(_player.Connection, _player.Abilities.CreatePacket(_player.UniqueId, _player.IsOperator));

        if (_player.Dimension?.World?.Server is Server server)
        {
            server.Commands.SendAvailableCommands(server, _player);
        }
    }

    public void SetOperator(bool isOperator, bool syncClient = true)
    {
        _player.IsOperator = isOperator;
        _player.Abilities.SetOperator(isOperator);

        if (isOperator)
        {
            Add("basalt.op", syncClient: false);
        }
        else
        {
            Remove("basalt.op", syncClient: false);
        }

        if (syncClient)
        {
            Sync();
        }
    }

    internal void RestoreOperator(bool isOperator)
    {
        _player.Abilities.SetOperator(isOperator);
        if (isOperator)
        {
            _permissions.Add("basalt.op");
        }
        else
        {
            _permissions.Remove("basalt.op");
        }
    }
}
