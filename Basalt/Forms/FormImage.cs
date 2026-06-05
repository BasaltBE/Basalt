namespace Basalt.Core.Forms;

public readonly record struct FormImage(FormImageType Type, string Data)
{
    internal object ToPayload()
    {
        return new
        {
            type = Type == FormImageType.Path ? "path" : "url",
            data = Data
        };
    }
}
