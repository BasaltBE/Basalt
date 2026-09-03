namespace Basalt.Core.Plugins;

public abstract class Plugin {
    public Server Server = null!;
    public PluginDescription Description = null!;
    public string AssemblyPath = string.Empty;

    public virtual void OnLoad() {
    }

    public virtual void OnStart() {
    }

    public virtual void OnDisable() {
    }
}
