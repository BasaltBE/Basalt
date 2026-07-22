namespace Basalt.Core.Plugins;

using McMaster.NETCore.Plugins;

public sealed class PluginContainer {
    public Plugin Plugin = null!;
    public PluginDescription Description = null!;
    public string AssemblyPath = string.Empty;
    public PluginLoader Loader = null!;
    public PluginState State;
}
