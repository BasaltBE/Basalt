namespace Basalt.Core.DDUI;

using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

public abstract class DataDrivenScreen
{
    readonly DduiLayout _layout;
    readonly HashSet<Player.Player> _viewers = [];
    uint _updateCount;

    internal DduiProperty Root = DduiProperty.Object(string.Empty);

    public abstract string Identifier { get; }
    public abstract string Property { get; }

    protected DataDrivenScreen()
    {
        _layout = new DduiLayout(this);
        Root.Set(_layout.Property);
    }

    public void Show(Player.Player player)
    {
        player.Screens[Property] = this;
        _viewers.Add(player);

        player.Send(
            CreateDataPacket(),
            new ClientboundDataDrivenUIShowScreenPacket
            {
                ScreenId = Identifier,
                FormId = 0
            });
    }

    public void Close(Player.Player player)
    {
        player.Screens.Remove(Property);
        _viewers.Remove(player);
        player.Send(new ClientboundDataDrivenUIClosePacket());
    }

    internal void Handle(Player.Player player, DataStoreUpdate update)
    {
        Resolve(update.Path)?.Trigger(player, update.Value);
    }

    private protected void Set(DduiProperty property)
    {
        Root.Set(property);
    }

    private protected void Add(DduiElement element)
    {
        _layout.Add(element.Property);
    }

    private protected void Listen(string path, Action<Player.Player, object> listener)
    {
        Resolve(path)?.Listen(listener);
    }

    internal string StoreName => Identifier.Split(':')[0];

    ClientboundDataStorePacket CreateDataPacket()
    {
        return new ClientboundDataStorePacket
        {
            Updates =
            [
                new DataStoreChange
                {
                    DataStoreName = StoreName,
                    Property = Property,
                    UpdateCount = ++_updateCount,
                    Value = Root.ToValue()
                }
            ]
        };
    }

    DduiProperty? Resolve(string path)
    {
        DduiProperty? target = Root;
        foreach (string segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            (string name, string? index) = Parse(segment);
            target = target.Get(name);
            if (target is null)
            {
                return null;
            }

            if (index is not null)
            {
                target = target.Get(index);
                if (target is null)
                {
                    return null;
                }
            }
        }

        return target;
    }

    static (string Name, string? Index) Parse(string segment)
    {
        int bracket = segment.IndexOf('[');
        if (bracket < 0 || !segment.EndsWith(']'))
        {
            return (segment, null);
        }

        return (segment[..bracket], segment[(bracket + 1)..^1]);
    }
}
