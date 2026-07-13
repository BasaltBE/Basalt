namespace Basalt.Core.Forms;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

public sealed class MessageForm : Form<bool?>
{
    public string Content;
    public string Button1;
    public string Button2;

    public MessageForm(string title, string content = "", string button1 = "OK", string button2 = "Cancel") : base(title)
    {
        Content = content;
        Button1 = button1;
        Button2 = button2;
    }

    protected override object CreatePayload()
    {
        return new
        {
            type = "modal",
            title = Title,
            content = Content,
            button1 = Button1,
            button2 = Button2
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "...")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "...")]
    protected override bool? ReadResponse(string? data)
    {
        return data is null ? null : JsonSerializer.Deserialize<bool?>(data);
    }
}
