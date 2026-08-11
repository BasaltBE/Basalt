using BedrockProtocol.Packets;
using BedrockProtocol.Types;

namespace Basalt.Core.DDUI;

public abstract class DataDrivenScreen {
    static uint _nextId;

    readonly DduiLayout _layout;
    readonly HashSet<Player.Player> _viewers = [];
    readonly uint _formId;
    readonly uint _dataInstanceId;
    uint _updateCount;
    bool _handled;

    internal DduiProperty Root = DduiProperty.Object(string.Empty);

    public abstract string Identifier { get; }

    public string Property { get; }

    protected DataDrivenScreen() {
        _formId = ++_nextId;
        _dataInstanceId = ++_nextId;
        Property = DeriveProperty(Identifier, _dataInstanceId);
        _layout = new DduiLayout(this);
        Root.Set(_layout.Property);
    }

    public void Show(Player.Player player) {
        DataDrivenScreen? existing = null;
        foreach ((string key, DataDrivenScreen screen) in player.Screens) {
            existing = screen;
            break;
        }

        if (existing is not null) {
            existing.Unregister(player);
            player.Screens[existing.Property] = this;
            _viewers.Add(player);

            player.Send(new ClientboundDataStorePacket {
                Updates =
                [
                    new DataStoreChange
                    {
                        DataStoreName = StoreName,
                        Property = existing.Property,
                        UpdateCount = existing._updateCount + 1,
                        TheNewPropertyValue = Root.ToValue()
                    }
                ]
            });
            return;
        }

        player.Screens[Property] = this;
        _viewers.Add(player);

        player.Send(
            CreateDataPacket(),
            new ClientboundDataDrivenUIShowScreenPacket {
                ScreenId = Identifier,
                FormId = _formId,
                DataInstanceId = _dataInstanceId
            });
    }

    public void Close(Player.Player player) {
        player.Screens.Remove(Property);
        _viewers.Remove(player);
    }

    /// <summary>
    /// Hides the form by updating it to show only a close button, which the client will auto-dismiss.
    /// </summary>
    internal void Hide(Player.Player player) {
        string? registeredProperty = null;
        uint updateCount = _updateCount;

        foreach ((string key, DataDrivenScreen screen) in player.Screens) {
            if (screen == this) {
                registeredProperty = key;
                break;
            }
        }

        if (registeredProperty is null) return;

        player.Screens.Remove(registeredProperty);
        _viewers.Remove(player);

        DduiProperty emptyRoot = DduiProperty.Object(string.Empty);
        DduiProperty layout = DduiProperty.Object("layout");
        layout.Set(DduiProperty.Long("length", 0));
        emptyRoot.Set(layout);
        emptyRoot.Set(DduiProperty.String("title", string.Empty));

        DduiProperty closeBtn = DduiProperty.Object("closeButton");
        closeBtn.Set(DduiProperty.String("label", string.Empty));
        closeBtn.Set(DduiProperty.Boolean("button_visible", false));
        closeBtn.Set(DduiProperty.Boolean("visible", false));
        closeBtn.Set(DduiProperty.Long("onClick", 0));
        emptyRoot.Set(closeBtn);

        player.Send(new ClientboundDataStorePacket {
            Updates =
            [
                new DataStoreChange
                {
                    DataStoreName = StoreName,
                    Property = registeredProperty,
                    UpdateCount = ++updateCount,
                    TheNewPropertyValue = emptyRoot.ToValue()
                }
            ]
        });
    }

    /// <summary>
    /// Unregisters the screen without sending a close packet. Used before opening a replacement screen.
    /// </summary>
    internal void Unregister(Player.Player player) {
        player.Screens.Remove(Property);
        _viewers.Remove(player);
    }

    /// <summary>
    /// Closes the screen visually by sending the close packet to the client.
    /// </summary>
    public void Dismiss(Player.Player player) {
        Close(player);
    }

    internal void Handle(Player.Player player, DataStoreUpdate update) {
        if (_handled) return;

        DduiProperty? target = Resolve(update.Path);
        if (target is null) return;

        bool isClick = update.Path.EndsWith("onClick");
        if (isClick)
            _handled = true;

        target.Trigger(player, update);
    }

    private protected void Set(DduiProperty property) {
        Root.Set(property);
    }

    private protected void Add(DduiElement element) {
        _layout.Add(element.Property);
    }

    private protected void Listen(string path, Func<Player.Player, object, bool> listener) {
        Resolve(path)?.Listen(listener);
    }

    internal static string StoreName => "minecraft";

    ClientboundDataStorePacket CreateDataPacket() {
        return new ClientboundDataStorePacket {
            Updates =
            [
                new DataStoreChange
                {
                    DataStoreName = StoreName,
                    Property = Property,
                    UpdateCount = ++_updateCount,
                    TheNewPropertyValue = Root.ToValue()
                }
            ]
        };
    }

    ClientboundDataStorePacket CreateCleanupPacket() {
        return new ClientboundDataStorePacket {
            Updates =
            [
                new DataStoreChange
                {
                    DataStoreName = StoreName,
                    Property = Property,
                    UpdateCount = ++_updateCount,
                    TheNewPropertyValue = DataStorePropertyValue.Null()
                }
            ]
        };
    }

    DduiProperty? Resolve(string path) {
        DduiProperty? target = Root;
        foreach (string segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries)) {
            (string name, string? index) = Parse(segment);
            target = target.Get(name);
            if (target is null) return null;

            if (index is not null) {
                target = target.Get(index);
                if (target is null) return null;
            }
        }

        return target;
    }

    static (string Name, string? Index) Parse(string segment) {
        int bracket = segment.IndexOf('[');
        if (bracket < 0 || !segment.EndsWith(']'))
            return (segment, null);

        return (segment[..bracket], segment[(bracket + 1)..^1]);
    }

    static string DeriveProperty(string screenId, uint dataInstanceId) {
        string baseName = screenId.StartsWith("minecraft:")
            ? screenId["minecraft:".Length..]
            : screenId;
        baseName = baseName.Replace(':', '_');
        return $"{baseName}_data_{dataInstanceId}";
    }
}
