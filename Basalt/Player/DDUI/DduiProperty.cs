namespace Basalt.Core.DDUI;

internal sealed class DduiProperty {
    readonly Dictionary<string, DduiProperty> _children = [];
    readonly List<Func<Player.Player, object, bool>> _listeners = [];

    public string Name;
    public DataStorePropertyValue Value;
    public DduiProperty? Parent;

    public DduiProperty(string name, DataStorePropertyValue value) {
        Name = name;
        Value = value;
    }

    public DduiProperty Set(DduiProperty property) {
        property.Parent = this;
        _children[property.Name] = property;
        return property;
    }

    public DduiProperty? Get(string name) {
        return _children.GetValueOrDefault(name);
    }

    public void Listen(Func<Player.Player, object, bool> listener) {
        _listeners.Add(listener);
    }

    /// <summary>
    /// Triggers all listeners. Returns true if any listener requests closing the screen.
    /// </summary>
    public bool Trigger(Player.Player player, object value) {
        bool shouldClose = false;
        for (int i = 0; i < _listeners.Count; i++) {
            if (_listeners[i](player, value))
                shouldClose = true;
        }
        return shouldClose;
    }

    public string Path {
        get {
            if (Parent is null)
                return Name;

            string parentPath = Parent.Path;
            if (Parent.Name.Length == 0)
                return Name;

            return int.TryParse(Name, out _) ? $"{parentPath}[{Name}]" : $"{parentPath}.{Name}";
        }
    }

    public DataStorePropertyValue ToValue() {
        if (Value.Type != Protocol.Enums.DataStorePropertyValueType.Type)
            return Value;

        Dictionary<string, DataStorePropertyValue> properties = [];
        foreach ((string name, DduiProperty property) in _children) {
            properties[name] = property.ToValue();
        }

        return DataStorePropertyValue.TypeValue(properties);
    }

    public static DduiProperty Object(string name) => new(name, DataStorePropertyValue.TypeValue([]));
    public static DduiProperty String(string name, string value) => new(name, DataStorePropertyValue.String(value));
    public static DduiProperty Boolean(string name, bool value) => new(name, DataStorePropertyValue.Boolean(value));
    public static DduiProperty Long(string name, long value) => new(name, DataStorePropertyValue.Int64(value));
    public static DduiProperty Double(string name, double value) => new(name, DataStorePropertyValue.Double(value));
}
