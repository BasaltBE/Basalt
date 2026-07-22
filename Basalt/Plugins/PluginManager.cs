namespace Basalt.Core.Plugins;

using System.Diagnostics;
using System.Reflection;
using Basalt.Core.Profiling;
using McMaster.NETCore.Plugins;

public sealed class PluginManager {
    private readonly Server _server;
    private readonly List<PluginContainer> _plugins = [];

    public IEnumerable<PluginContainer> Plugins => _plugins;

    public PluginManager(Server server) {
        _server = server;
    }

    public void LoadAll(string directory) {
        using var __zone = Profiler.BeginZone("Plugins.LoadAll");
        long LoadPluginsTimeStamp = Stopwatch.GetTimestamp();

        string absoluteDirectory = Path.GetFullPath(directory);
        if (!Directory.Exists(absoluteDirectory)) {
            Directory.CreateDirectory(absoluteDirectory);
            return;
        }

        int count = 0;

        foreach (string subDir in Directory.GetDirectories(absoluteDirectory)) {
            string pluginName = Path.GetFileName(subDir);
            string pluginDll = Path.Combine(subDir, $"{pluginName}.dll");
            if (File.Exists(pluginDll)) {
                Load(pluginDll);
                count += 1;
            }
        }

        TimeSpan LoadPluginsElapsed = Stopwatch.GetElapsedTime(LoadPluginsTimeStamp);
        Logger.Info($"Loaded {count} plugins in {LoadPluginsElapsed.Milliseconds}ms.");
    }

    public void Load(string assemblyPath) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone($"Plugin.Load({Path.GetFileName(assemblyPath)})") : default;
        try {
            var loader = PluginLoader.CreateFromAssemblyFile(
                assemblyPath,
                sharedTypes: [typeof(Plugin), typeof(Server)]
            );

            Assembly assembly = loader.LoadDefaultAssembly();
            PluginAttribute? attribute = assembly.GetCustomAttribute<PluginAttribute>();
            if (attribute is null) {
                return;
            }

            Type entry = GetEntry(assembly);
            if (Activator.CreateInstance(entry) is not Plugin plugin) {
                throw new InvalidOperationException($"Plugin entry '{entry.FullName}' could not be created.");
            }

            PluginDescription description = PluginDescription.From(attribute);
            plugin.Server = _server;
            plugin.Description = description;
            plugin.AssemblyPath = assemblyPath;

            plugin.OnLoad();

            _plugins.Add(new PluginContainer {
                Plugin = plugin,
                Description = description,
                AssemblyPath = assemblyPath,
                Loader = loader,
                State = PluginState.Loaded
            });
        }
        catch (Exception exception) {
            Logger.Warn($"Failed to load plugin '{Path.GetFileName(assemblyPath)}': {exception.Message}");
        }
    }

    public void StartAll() {
        foreach (PluginContainer plugin in _plugins) {
            if (plugin.State != PluginState.Loaded) {
                continue;
            }

            try {
                plugin.Plugin.OnStart();
                plugin.State = PluginState.Started;
            }
            catch (Exception exception) {
                plugin.State = PluginState.Failed;
                Logger.Warn($"Failed to start plugin '{plugin.Description.Name}': {exception.Message}");
            }
        }
    }

    public void DisableAll() {
        for (int i = _plugins.Count - 1; i >= 0; i--) {
            PluginContainer plugin = _plugins[i];
            if (plugin.State != PluginState.Started) {
                continue;
            }

            try {
                plugin.Plugin.OnDisable();
                plugin.State = PluginState.Disabled;
            }
            catch (Exception exception) {
                plugin.State = PluginState.Failed;
                Logger.Warn($"Failed to disable plugin '{plugin.Description.Name}': {exception.Message}");
            }
        }
    }

    private static Type GetEntry(Assembly assembly) {
        Type[] entries = assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(Plugin).IsAssignableFrom(type))
            .ToArray();

        if (entries.Length == 0) {
            throw new InvalidOperationException("Plugin assembly does not contain a Plugin type.");
        }

        if (entries.Length > 1) {
            throw new InvalidOperationException("Plugin assembly contains multiple Plugin types.");
        }

        return entries[0];
    }
}
