namespace Basalt.Core.Events;

using Basalt.Core.Plugins;

internal abstract class SignalHandler {
    public PluginContainer? Plugin { get; }

    protected SignalHandler(PluginContainer? plugin) {
        Plugin = plugin;
    }

    public abstract void Invoke(ISignal signal);
}
