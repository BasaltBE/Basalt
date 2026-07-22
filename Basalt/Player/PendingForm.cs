namespace Basalt.Core.Player;

internal sealed class PendingForm {
    private readonly Action<string?, bool> _form;

    public PendingForm(Action<string?, bool> form) {
        _form = form;
    }

    public void Complete(string? data, bool canceled) {
        _form.Invoke(data, canceled);
    }
}
