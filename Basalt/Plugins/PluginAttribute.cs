namespace Basalt.Core.Plugins;

[AttributeUsage(AttributeTargets.Assembly)]
public sealed class PluginAttribute : Attribute
{
    public string Name;
    public string Version;
    public string[] Authors = [];

    public PluginAttribute(string name, string version)
    {
        Name = name;
        Version = version;
    }
}
