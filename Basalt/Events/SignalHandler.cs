namespace Basalt.Core.Events;

internal abstract class SignalHandler {
    public abstract void Invoke(ISignal signal);
}
