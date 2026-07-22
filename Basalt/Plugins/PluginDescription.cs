namespace Basalt.Core.Plugins;

public sealed class PluginDescription {
    public string Name = string.Empty;
    public string Version = string.Empty;
    public string[] Authors = [];

    public static PluginDescription From(PluginAttribute attribute) {
        return new PluginDescription {
            Name = attribute.Name,
            Version = attribute.Version,
            Authors = attribute.Authors
        };
    }
}
