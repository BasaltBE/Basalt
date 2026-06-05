namespace Basalt.Core.Forms;

internal sealed class FormParticipant
{
    private readonly Action<string?, bool> _complete;

    public FormParticipant(Action<string?, bool> complete)
    {
        _complete = complete;
    }

    public void Complete(string? data, bool canceled)
    {
        _complete(data, canceled);
    }
}
