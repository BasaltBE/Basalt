namespace Basalt.Core.Plugins;

using System.Collections.Concurrent;
using Basalt.Core.Tasks;

public sealed class PluginContainer {
    public Plugin Plugin = null!;
    public PluginDescription Description = null!;
    public string AssemblyPath = string.Empty;
    internal PluginAssemblyLoadContext Loader = null!;
    public PluginState State;
    public int RuntimeFailures;
    internal ConcurrentDictionary<ServerTask, byte> Tasks { get; } = new();
}
