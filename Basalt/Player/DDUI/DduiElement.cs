namespace Basalt.Core.DDUI;

internal sealed class DduiElement
{
    public DduiProperty Property;
    public DduiProperty? ClickProperty;

    DduiElement(string name)
    {
        Property = DduiProperty.Object(name);
    }

    public static DduiElement Button(string label, Action<Player.Player> click, string tooltip = "", bool visible = true, bool disabled = false)
    {
        DduiElement element = new("button");
        element.Property.Set(DduiProperty.String("label", label));
        element.Property.Set(DduiProperty.String("tooltip", tooltip));
        element.Property.Set(DduiProperty.Boolean("visible", visible));
        element.Property.Set(DduiProperty.Boolean("disabled", disabled));
        element.Property.Set(DduiProperty.Boolean("button_visible", visible));
        DduiProperty onClick = element.Property.Set(DduiProperty.Long("onClick", 0));
        element.ClickProperty = onClick;
        onClick.Listen((player, _) => click(player));
        return element;
    }

    public static DduiElement Label(string text, bool visible = true)
    {
        DduiElement element = new("label");
        element.Property.Set(DduiProperty.String("text", text));
        element.Property.Set(DduiProperty.Boolean("visible", visible));
        element.Property.Set(DduiProperty.Boolean("header_visible", visible));
        return element;
    }

    public static DduiElement Divider(bool visible = true)
    {
        DduiElement element = new("divider");
        element.Property.Set(DduiProperty.Boolean("visible", visible));
        element.Property.Set(DduiProperty.Boolean("divider_visible", visible));
        return element;
    }

    public static DduiElement Spacer(bool visible = true)
    {
        DduiElement element = new("spacer");
        element.Property.Set(DduiProperty.Boolean("visible", visible));
        element.Property.Set(DduiProperty.Boolean("spacer_visible", visible));
        return element;
    }

    public static DduiElement CloseButton(string label, bool visible, Action<Player.Player> click)
    {
        DduiElement element = new("closeButton");
        element.Property.Set(DduiProperty.String("label", label));
        element.Property.Set(DduiProperty.Boolean("visible", visible));
        element.Property.Set(DduiProperty.Boolean("button_visible", visible));
        DduiProperty onClick = element.Property.Set(DduiProperty.Long("onClick", 0));
        element.ClickProperty = onClick;
        onClick.Listen((player, _) => click(player));
        return element;
    }
}
