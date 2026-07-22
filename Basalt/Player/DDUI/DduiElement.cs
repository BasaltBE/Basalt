namespace Basalt.Core.DDUI;

internal sealed class DduiElement {
    public DduiProperty Property;

    DduiElement(string name) {
        Property = DduiProperty.Object(name);
    }

    public static DduiElement Button(string label, Func<Player.Player, bool> click, string tooltip = "", bool visible = true) {
        DduiElement element = new("button");
        element.Property.Set(DduiProperty.String("label", label));
        element.Property.Set(DduiProperty.Boolean("button_visible", visible));
        element.Property.Set(DduiProperty.Boolean("visible", visible));

        if (tooltip.Length > 0) {
            element.Property.Set(DduiProperty.String("tooltip", tooltip));
            element.Property.Set(DduiProperty.Boolean("tooltip_visible", true));
        }

        DduiProperty onClick = element.Property.Set(DduiProperty.Long("onClick", 0));
        onClick.Listen((player, _) => click(player));
        return element;
    }

    public static DduiElement Label(string text, bool visible = true) {
        DduiElement element = new("label");
        element.Property.Set(DduiProperty.String("text", text));
        element.Property.Set(DduiProperty.Boolean("label_visible", visible));
        element.Property.Set(DduiProperty.Boolean("visible", visible));
        return element;
    }

    public static DduiElement Divider(bool visible = true) {
        DduiElement element = new("divider");
        element.Property.Set(DduiProperty.Boolean("divider_visible", visible));
        element.Property.Set(DduiProperty.Boolean("visible", visible));
        return element;
    }

    public static DduiElement Spacer(bool visible = true) {
        DduiElement element = new("spacer");
        element.Property.Set(DduiProperty.Boolean("spacer_visible", visible));
        element.Property.Set(DduiProperty.Boolean("visible", visible));
        return element;
    }

    public static DduiElement TextField(string label, string defaultValue, Action<Player.Player, string> onChange, string description = "", bool visible = true) {
        DduiElement element = new("textfield");
        element.Property.Set(DduiProperty.String("label", label));
        element.Property.Set(DduiProperty.String("text", defaultValue));
        element.Property.Set(DduiProperty.String("description", description));
        element.Property.Set(DduiProperty.Boolean("textfield_visible", visible));
        element.Property.Set(DduiProperty.Boolean("visible", visible));

        DduiProperty textProp = element.Property.Get("text")!;
        textProp.Listen((player, value) => {
            if (value is string str) onChange(player, str);
            return false;
        });
        return element;
    }

    public static DduiElement Toggle(string label, bool defaultValue, Action<Player.Player, bool> onToggle, bool visible = true) {
        DduiElement element = new("toggle");
        element.Property.Set(DduiProperty.String("label", label));
        element.Property.Set(DduiProperty.Boolean("toggled", defaultValue));
        element.Property.Set(DduiProperty.Boolean("toggle_visible", visible));
        element.Property.Set(DduiProperty.Boolean("visible", visible));

        DduiProperty toggledProp = element.Property.Get("toggled")!;
        toggledProp.Listen((player, value) => {
            if (value is bool b) onToggle(player, b);
            return false;
        });
        return element;
    }

    public static DduiElement Dropdown(string label, string[] options, int defaultIndex, Action<Player.Player, int> onSelect, bool visible = true) {
        DduiElement element = new("dropdown");
        element.Property.Set(DduiProperty.String("label", label));
        element.Property.Set(DduiProperty.Boolean("dropdown_visible", visible));
        element.Property.Set(DduiProperty.Boolean("visible", visible));
        element.Property.Set(DduiProperty.Long("value", defaultIndex));

        DduiProperty items = element.Property.Set(DduiProperty.Object("items"));
        for (int i = 0; i < options.Length; i++) {
            DduiProperty item = items.Set(DduiProperty.Object(i.ToString()));
            item.Set(DduiProperty.String("label", options[i]));
            item.Set(DduiProperty.Long("value", i));
        }
        items.Set(DduiProperty.Long("length", options.Length));

        DduiProperty valueProp = element.Property.Get("value")!;
        valueProp.Listen((player, value) => {
            int index = value switch {
                long l => (int)l,
                double d => (int)d,
                _ => -1
            };
            if (index >= 0) onSelect(player, index);
            return false;
        });
        return element;
    }

    public static DduiElement Slider(string label, double defaultValue, double min, double max, double step, Action<Player.Player, double> onChange, string description = "", bool visible = true) {
        DduiElement element = new("slider");
        element.Property.Set(DduiProperty.String("label", label));
        element.Property.Set(DduiProperty.Double("value", defaultValue));
        element.Property.Set(DduiProperty.Double("minValue", min));
        element.Property.Set(DduiProperty.Double("maxValue", max));
        element.Property.Set(DduiProperty.Double("step", step));
        element.Property.Set(DduiProperty.String("description", description));
        element.Property.Set(DduiProperty.Boolean("slider_visible", visible));
        element.Property.Set(DduiProperty.Boolean("visible", visible));

        DduiProperty valueProp = element.Property.Get("value")!;
        valueProp.Listen((player, value) => {
            double v = value switch {
                double d => d,
                long l => l,
                _ => 0
            };
            onChange(player, v);
            return false;
        });
        return element;
    }

    public static DduiElement CloseButton(string label, Func<Player.Player, bool> click, bool visible = true) {
        DduiElement element = new("closeButton");
        element.Property.Set(DduiProperty.String("label", label));
        element.Property.Set(DduiProperty.Boolean("button_visible", visible));
        element.Property.Set(DduiProperty.Boolean("visible", visible));
        DduiProperty onClick = element.Property.Set(DduiProperty.Long("onClick", 0));
        onClick.Listen((player, _) => click(player));
        return element;
    }
}
