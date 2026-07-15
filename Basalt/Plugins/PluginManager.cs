namespace Basalt.Core.Plugins;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Basalt.Core.Profiling;

public sealed class PluginManager
{
    private readonly Server _server;
    private readonly List<PluginContainer> _plugins = [];

    public IEnumerable<PluginContainer> Plugins => _plugins;

    public PluginManager(Server server)
    {
        _server = server;
    }

    [RequiresUnreferencedCode("Plugin loading uses Assembly.LoadFrom")]
    public void LoadAll(string directory)
    {
        using var __zone = Profiler.BeginZone("Plugins.LoadAll");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            return;
        }

        foreach (string assemblyPath in Directory.GetFiles(directory, "*.dll"))
        {
            Load(assemblyPath);
        }
    }

    [RequiresUnreferencedCode("Plugin loading uses Assembly.LoadFrom")]
    public void Load(string assemblyPath)
    {
        using var __zone = Profiler.BeginZone($"Plugin.Load({Path.GetFileName(assemblyPath)})");
        try
        {
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            PluginAttribute? attribute = assembly.GetCustomAttribute<PluginAttribute>();
            if (attribute is null)
            {
                return;
            }

            Type entry = GetEntry(assembly);
            if (Activator.CreateInstance(entry) is not Plugin plugin)
            {
                throw new InvalidOperationException($"Plugin entry '{entry.FullName}' could not be created.");
            }

            PluginDescription description = PluginDescription.From(attribute);
            plugin.Server = _server;
            plugin.Description = description;
            plugin.AssemblyPath = assemblyPath;

            plugin.OnLoad();

            _plugins.Add(new PluginContainer
            {
                Plugin = plugin,
                Description = description,
                AssemblyPath = assemblyPath,
                State = PluginState.Loaded
            });
            // Logger.Info($"Loaded plugin {description.Name} {description.Version}.");
        }
        catch (Exception exception)
        {
            Logger.Warn($"Failed to load plugin '{Path.GetFileName(assemblyPath)}': {exception.Message}");
        }
    }

    public void StartAll()
    {
        foreach (PluginContainer plugin in _plugins)
        {
            if (plugin.State != PluginState.Loaded)
            {
                continue;
            }

            try
            {
                plugin.Plugin.OnStart();
                plugin.State = PluginState.Started;
                // Logger.Info($"Started plugin {plugin.Description.Name}.");
            }
            catch (Exception exception)
            {
                plugin.State = PluginState.Failed;
                Logger.Warn($"Failed to start plugin '{plugin.Description.Name}': {exception.Message}");
            }
        }
    }

    public void DisableAll()
    {
        for (int i = _plugins.Count - 1; i >= 0; i--)
        {
            PluginContainer plugin = _plugins[i];
            if (plugin.State != PluginState.Started)
            {
                continue;
            }

            try
            {
                plugin.Plugin.OnDisable();
                plugin.State = PluginState.Disabled;
                // Logger.Info($"Disabled plugin {plugin.Description.Name}.");
            }
            catch (Exception exception)
            {
                plugin.State = PluginState.Failed;
                Logger.Warn($"Failed to disable plugin '{plugin.Description.Name}': {exception.Message}");
            }
        }
    }

    [RequiresUnreferencedCode("...")]
    private static Type GetEntry(Assembly assembly)
    {
        Type[] entries = assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(Plugin).IsAssignableFrom(type))
            .ToArray();

        if (entries.Length == 0)
        {
            throw new InvalidOperationException("Plugin assembly does not contain a Plugin type.");
        }

        if (entries.Length > 1)
        {
            throw new InvalidOperationException("Plugin assembly contains multiple Plugin types.");
        }

        return entries[0];
    }
}
