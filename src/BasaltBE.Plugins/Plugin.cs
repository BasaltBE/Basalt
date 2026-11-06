using BasaltBE.Logging;
using BasaltBE.Basalt;
using BasaltBE.Plugins.Packaging;

namespace BasaltBE.Plugins;

public abstract class Plugin {
    public PluginManifest Manifest { get; internal set; } = null!;
    public Server Server { get; internal set; } = null!;
    public ScopedLogger Logger { get; internal set; }
    public string DataDirectory { get; internal set; } = "";

    public virtual void OnLoad() { }
    public virtual void OnEnable() { }
    public virtual void OnDisable() { }
}
