namespace Basalt.Core.Events;

internal sealed class TypedSignalHandler<TSignal> : SignalHandler where TSignal : ISignal {
    private readonly Action<TSignal> _handler;

    public TypedSignalHandler(Action<TSignal> handler) {
        _handler = handler;
    }

    public override void Invoke(ISignal signal) {
        if (signal is TSignal typed) {
            _handler(typed);
        }
    }
}
