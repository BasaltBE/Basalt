namespace Basalt.Core.DDUI;

public sealed class CustomForm : DataDrivenScreen
{
    public override string Identifier => "minecraft:custom_form";

    public CustomForm(string? title = null)
    {
        if (title is not null)
            Title(title);
    }

    public CustomForm Title(string title)
    {
        Set(DduiProperty.String("title", title));
        return this;
    }

    public CustomForm Label(string text)
    {
        Add(DduiElement.Label(text));
        return this;
    }

    public CustomForm Divider()
    {
        Add(DduiElement.Divider());
        return this;
    }

    public CustomForm Spacer()
    {
        Add(DduiElement.Spacer());
        return this;
    }

    /// <summary>
    /// Adds a button. The click callback returns true to close the form, false to keep it open.
    /// </summary>
    public CustomForm Button(string label, Func<Player.Player, bool> click)
    {
        Add(DduiElement.Button(label, click));
        return this;
    }

    /// <summary>
    /// Adds a button that closes the form on click.
    /// </summary>
    public CustomForm Button(string label, Action<Player.Player> click)
    {
        Add(DduiElement.Button(label, player =>
        {
            click(player);
            return true;
        }));
        return this;
    }

    /// <summary>
    /// Adds a button with tooltip. The click callback returns true to close the form.
    /// </summary>
    public CustomForm Button(string label, string tooltip, Func<Player.Player, bool> click)
    {
        Add(DduiElement.Button(label, click, tooltip));
        return this;
    }

    /// <summary>
    /// Adds a button with tooltip that closes on click.
    /// </summary>
    public CustomForm Button(string label, string tooltip, Action<Player.Player> click)
    {
        Add(DduiElement.Button(label, player =>
        {
            click(player);
            return true;
        }, tooltip));
        return this;
    }

    public CustomForm TextField(string label, string defaultValue, Action<Player.Player, string> onChange, string description = "")
    {
        Add(DduiElement.TextField(label, defaultValue, onChange, description));
        return this;
    }

    public CustomForm Toggle(string label, bool defaultValue, Action<Player.Player, bool> onToggle)
    {
        Add(DduiElement.Toggle(label, defaultValue, onToggle));
        return this;
    }

    public CustomForm Dropdown(string label, string[] options, int defaultIndex, Action<Player.Player, int> onSelect)
    {
        Add(DduiElement.Dropdown(label, options, defaultIndex, onSelect));
        return this;
    }

    public CustomForm Slider(string label, double defaultValue, double min, double max, double step, Action<Player.Player, double> onChange, string description = "")
    {
        Add(DduiElement.Slider(label, defaultValue, min, max, step, onChange, description));
        return this;
    }

    public CustomForm CloseButton(string label = "Close")
    {
        DduiElement element = DduiElement.CloseButton(label, player =>
        {
            Close(player);
            return false;
        });
        Set(element.Property);
        return this;
    }
}
