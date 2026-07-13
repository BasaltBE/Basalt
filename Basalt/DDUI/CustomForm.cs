namespace Basalt.Core.DDUI;

public sealed class CustomForm : DataDrivenScreen
{
    public override string Identifier => "minecraft:custom_form";
    public override string Property => "custom_form_data";

    public CustomForm(string? title = null)
    {
        if (title is not null)
        {
            Title(title);
        }
    }

    public CustomForm Title(string title)
    {
        Set(DduiProperty.String("title", title));
        return this;
    }

    public CustomForm Button(string label, Action<Player.Player> click)
    {
        Add(DduiElement.Button(label, click));
        return this;
    }

    public CustomForm Button(string label, string tooltip, Action<Player.Player> click)
    {
        Add(DduiElement.Button(label, click, tooltip));
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

    public CustomForm CloseButton(string label = "Close")
    {
        DduiElement element = DduiElement.CloseButton(label, true, Close);
        Set(element.Property);
        return this;
    }
}
