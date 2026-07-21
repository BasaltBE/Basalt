namespace Basalt.Core.DDUI;

using Basalt.Protocol.Packets;
using Basalt.Protocol.Types;

public abstract class DataDrivenScreen
{
    static uint _nextId;

    readonly DduiLayout _layout;
    readonly HashSet<Player.Player> _viewers = [];
    readonly uint _formId;
    readonly uint _dataInstanceId;
    uint _updateCount;

    internal DduiProperty Root = DduiProperty.Object(string.Empty);

    public abstract string Identifier { get; }

    public string Property { get; }

    protected DataDrivenScreen()
    {
        _formId = ++_nextId;
        _dataInstanceId = ++_nextId;
        Property = DeriveProperty(Identifier, _dataInstanceId);
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
                FormId = _formId,
                DataInstanceId = _dataInstanceId
            });
    }

    public void Close(Player.Player player)
    {
        player.Screens.Remove(Property);
        _viewers.Remove(player);
        player.Send(
            new ClientboundDataDrivenUIClosePacket { FormId = _formId },
            CreateCleanupPacket());
    }

    internal void Handle(Player.Player player, DataStoreUpdate update)
    {
        DduiProperty? target = Resolve(update.Path);
        if (target is null) return;

        bool shouldClose = target.Trigger(player, update.Value);
        if (shouldClose)
        {
            Close(player);
        }
    }

    private protected void Set(DduiProperty property)
    {
        Root.Set(property);
    }

    private protected void Add(DduiElement element)
    {
        _layout.Add(element.Property);
    }

    private protected void Listen(string path, Func<Player.Player, object, bool> listener)
    {
        Resolve(path)?.Listen(listener);
    }

    internal static string StoreName => "minecraft";

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

    ClientboundDataStorePacket CreateCleanupPacket()
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
                    Value = DataStorePropertyValue.Null()
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
            if (target is null) return null;

            if (index is not null)
            {
                target = target.Get(index);
                if (target is null) return null;
            }
        }

        return target;
    }

    static (string Name, string? Index) Parse(string segment)
    {
        int bracket = segment.IndexOf('[');
        if (bracket < 0 || !segment.EndsWith(']'))
            return (segment, null);

        return (segment[..bracket], segment[(bracket + 1)..^1]);
    }

    static string DeriveProperty(string screenId, uint dataInstanceId)
    {
        string baseName = screenId.StartsWith("minecraft:")
            ? screenId["minecraft:".Length..]
            : screenId;
        baseName = baseName.Replace(':', '_');
        return $"{baseName}_data_{dataInstanceId}";
    }
}
