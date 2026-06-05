namespace Basalt.Core.Forms;

using System.Text.Json;

public sealed class ActionForm : Form<int?>
{
    private readonly List<ActionFormButton> _buttons = [];
    public string Content;

    public ActionForm(string title, string content = "") : base(title)
    {
        Content = content;
    }

    public ActionForm Button(string text, FormImage? image = null)
    {
        _buttons.Add(new ActionFormButton(text, image));
        return this;
    }

    public void ClearButtons()
    {
        _buttons.Clear();
    }

    protected override object CreatePayload()
    {
        return new
        {
            type = "form",
            title = Title,
            content = Content,
            buttons = _buttons.Select(button => button.ToPayload()).ToArray()
        };
    }

    protected override int? ReadResponse(string? data)
    {
        return data is null ? null : JsonSerializer.Deserialize<int?>(data);
    }

    private readonly record struct ActionFormButton(string Text, FormImage? Image)
    {
        public object ToPayload()
        {
            if (Image is null)
            {
                return new { text = Text };
            }

            return new
            {
                text = Text,
                image = Image.Value.ToPayload()
            };
        }
    }
}
