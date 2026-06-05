namespace Basalt.Core.Plugins;

public sealed class PluginContainer
{
    public Plugin Plugin = null!;
    public PluginDescription Description = null!;
    public string AssemblyPath = string.Empty;
    public PluginState State;
}
