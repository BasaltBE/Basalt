namespace Basalt.Core.Plugins;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using Basalt.Core.Profiling;
using Basalt.Core.Tasks;

public sealed class PluginManager {
    private const int RuntimeFailureLimit = 3;
    private readonly Server _server;
    private readonly List<PluginContainer> _plugins = [];
    private readonly AsyncLocal<PluginContainer?> _registrationPlugin = new();
    private readonly IReadOnlySet<string> _sharedAssemblyNames = CreateSharedAssemblyNames();

    public IEnumerable<PluginContainer> Plugins => _plugins;
    internal PluginContainer? CurrentRegistrationPlugin => _registrationPlugin.Value;

    public PluginManager(Server server) {
        _server = server;
    }

    public void LoadAll(string directory) {
        using var __zone = Profiler.Enabled ? Profiler.BeginZone("Plugins.LoadAll") : default;
        long loadPluginsTimestamp = Stopwatch.GetTimestamp();

        string absoluteDirectory = Path.GetFullPath(directory);
        if (!Directory.Exists(absoluteDirectory)) {
            Directory.CreateDirectory(absoluteDirectory);
            return;
        }

        string temporaryDirectory = Path.Combine(absoluteDirectory, ".temp");
        Directory.CreateDirectory(temporaryDirectory);

        PluginCandidate[] candidates = [
            .. Directory.GetDirectories(absoluteDirectory)
                .Where(path => !Path.GetFileName(path).Equals(".temp", StringComparison.OrdinalIgnoreCase))
                .Select((path, order) => new PluginCandidate(
                    order,
                    Path.GetFileName(path),
                    Path.Combine(path, $"{Path.GetFileName(path)}.dll")))
                .Where(candidate => File.Exists(candidate.AssemblyPath))
        ];

        ConcurrentBag<PreparedPlugin> prepared = [];
        Parallel.ForEach(candidates, candidate => {
            try {
                prepared.Add(Prepare(candidate, temporaryDirectory));
            }
            catch (Exception exception) {
                LogLoadFailure(candidate.AssemblyPath, exception);
            }
        });

        int count = InitializePreparedPlugins(prepared);

        TimeSpan loadPluginsElapsed = Stopwatch.GetElapsedTime(loadPluginsTimestamp);
        Logger.Info($"Loaded {count} plugins in {loadPluginsElapsed.TotalMilliseconds:0}ms.");
        _ = Task.Run(() => CleanupStagedDirectories(temporaryDirectory));
    }

    public void Load(string assemblyPath) {
        string temporaryDirectory = Path.Combine(Path.GetDirectoryName(assemblyPath)!, ".temp");
        Directory.CreateDirectory(temporaryDirectory);
        PluginCandidate candidate = new(0, Path.GetFileNameWithoutExtension(assemblyPath), assemblyPath);

        try {
            PreparedPlugin plugin = Prepare(candidate, temporaryDirectory, stage: false);
            Initialize(plugin);
        }
        catch (Exception exception) {
            LogLoadFailure(assemblyPath, exception);
        }
    }

    public void StartAll() {
        foreach (PluginContainer plugin in _plugins.ToArray()) {
            if (plugin.State != PluginState.Loaded)
                continue;

            try {
                using (EnterRegistrationScope(plugin))
                    plugin.Plugin.OnStart();
                plugin.State = PluginState.Started;
            }
            catch (Exception exception) {
                plugin.State = PluginState.Failed;
                _server.RemovePluginHandlers(plugin);
                _server.Commands.RemovePluginCommands(plugin);
                _server.RemovePluginRegistrations(plugin);
                CancelTasks(plugin);
                Unload(plugin);
                Logger.Warn($"Failed to start plugin '{plugin.Description.Name}': {exception}");
            }
        }
    }

    public void DisableAll() {
        for (int i = _plugins.Count - 1; i >= 0; i--) {
            PluginContainer plugin = _plugins[i];
            if (plugin.State == PluginState.Loaded) {
                plugin.State = PluginState.Disabled;
                _server.RemovePluginHandlers(plugin);
                _server.Commands.RemovePluginCommands(plugin);
                _server.RemovePluginRegistrations(plugin);
                CancelTasks(plugin);
                Unload(plugin);
                continue;
            }

            if (plugin.State != PluginState.Started)
                continue;

            Disable(plugin);
        }
    }

    internal IDisposable EnterRegistrationScope(PluginContainer? plugin) {
        PluginContainer? previous = _registrationPlugin.Value;
        _registrationPlugin.Value = plugin;
        return new RegistrationScope(this, previous);
    }

    internal void ConfigureTask(ServerTask task, PluginContainer? owner) {
        if (task.Owner is null)
            task.Owner = owner;

        if (task.Owner is not { } plugin)
            return;

        task.RuntimeErrorHandler = RecordRuntimeFailure;
        task.CompletionHandler = UntrackTask;
        plugin.Tasks.TryAdd(task, 0);
    }

    private static void UntrackTask(ServerTask task) {
        task.Owner?.Tasks.TryRemove(task, out _);
    }

    private static void CancelTasks(PluginContainer plugin) {
        foreach (ServerTask task in plugin.Tasks.Keys)
            task.Cancel();
        plugin.Tasks.Clear();
    }

    internal void RecordRuntimeFailure(
        PluginContainer? plugin,
        ServerEvent @event,
        Exception exception) {
        RecordRuntimeFailure(plugin, @event.ToString(), exception);
    }

    internal void RecordRuntimeFailure(
        PluginContainer? plugin,
        string callback,
        Exception exception) {
        if (plugin is null) {
            Logger.Error($"Unhandled server callback error for '{callback}': {exception}");
            return;
        }

        int failures = Interlocked.Increment(ref plugin.RuntimeFailures);
        if (failures == 1 || failures >= RuntimeFailureLimit) {
            Logger.Warn(
                $"Plugin {plugin.Description.Name} failed during '{callback}' " +
                $"({failures}/{RuntimeFailureLimit}): {exception}");
        }

        if (failures >= RuntimeFailureLimit)
            Disable(plugin);
    }

    private PreparedPlugin Prepare(
        PluginCandidate candidate,
        string temporaryDirectory,
        bool stage = true) {
        string stagedAssemblyPath = stage
            ? StagePlugin(candidate, temporaryDirectory)
            : candidate.AssemblyPath;
        long loaderStart = Stopwatch.GetTimestamp();
        PluginAssemblyLoadContext loader = new(stagedAssemblyPath, _sharedAssemblyNames);
        double loaderMilliseconds = Stopwatch.GetElapsedTime(loaderStart).TotalMilliseconds;
        try {
            long assemblyStart = Stopwatch.GetTimestamp();
            Assembly assembly = loader.LoadFromAssemblyPath(stagedAssemblyPath);
            double assemblyMilliseconds = Stopwatch.GetElapsedTime(assemblyStart).TotalMilliseconds;
            long reflectionStart = Stopwatch.GetTimestamp();
            PluginAttribute? attribute = assembly.GetCustomAttribute<PluginAttribute>();
            if (attribute is null)
                throw new InvalidOperationException("Plugin assembly is missing PluginAttribute.");

            Type entry = GetEntry(assembly, attribute);
            double reflectionMilliseconds = Stopwatch.GetElapsedTime(reflectionStart).TotalMilliseconds;
            return new PreparedPlugin(
                candidate.Order,
                stagedAssemblyPath,
                loader,
                attribute,
                entry,
                loaderMilliseconds,
                assemblyMilliseconds,
                reflectionMilliseconds);
        }
        catch {
            loader.Unload();
            throw;
        }
    }

    private bool Initialize(PreparedPlugin prepared) {
        long loadStart = Stopwatch.GetTimestamp();
        PluginContainer container = new() {
            AssemblyPath = prepared.AssemblyPath,
            Description = PluginDescription.From(prepared.Attribute),
            Loader = prepared.Loader,
            State = PluginState.Loaded
        };

        try {
            if (Activator.CreateInstance(prepared.Entry) is not Plugin plugin)
                throw new InvalidOperationException($"Plugin entry '{prepared.Entry.FullName}' could not be created.");

            plugin.Server = _server;
            plugin.Description = container.Description;
            plugin.AssemblyPath = prepared.AssemblyPath;
            container.Plugin = plugin;

            using (EnterRegistrationScope(container))
                plugin.OnLoad();

            double onLoadMilliseconds = Stopwatch.GetElapsedTime(loadStart).TotalMilliseconds;
            // Logger.Info(
            //     $"Plugin {container.Description.Name}: loader~{prepared.LoaderMilliseconds:0}ms, " +
            //     $"assembly~{prepared.AssemblyMilliseconds:0}ms, " +
            //     $"reflection~{prepared.ReflectionMilliseconds:0}ms, " +
            //     $"OnLoad~{onLoadMilliseconds:0}ms.");

            _plugins.Add(container);
            return true;
        }
        catch (Exception exception) {
            _server.RemovePluginHandlers(container);
            _server.Commands.RemovePluginCommands(container);
            _server.RemovePluginRegistrations(container);
            CancelTasks(container);
            Unload(container);
            Logger.Warn($"Failed to load plugin '{prepared.AssemblyPath}': {exception}");
            if (_server.Properties.CrashOnPluginLoadFailure)
                throw;
            return false;
        }
    }

    private int InitializePreparedPlugins(IEnumerable<PreparedPlugin> preparedPlugins) {
        List<PreparedPlugin> remaining = [.. preparedPlugins.OrderBy(plugin => plugin.Order)];
        HashSet<string> knownNames = new(
            remaining.Select(plugin => plugin.Attribute.Name),
            StringComparer.OrdinalIgnoreCase);
        HashSet<string> loadedNames = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> failedNames = new(StringComparer.OrdinalIgnoreCase);
        int count = 0;

        while (remaining.Count > 0) {
            bool progressed = false;
            for (int i = 0; i < remaining.Count; i++) {
                PreparedPlugin plugin = remaining[i];
                string[] dependencies = plugin.Attribute.Dependencies;
                bool missingDependency = dependencies.Any(dependency =>
                    !knownNames.Contains(dependency) || failedNames.Contains(dependency));
                if (missingDependency) {
                    Discard(plugin);
                    failedNames.Add(plugin.Attribute.Name);
                    remaining.RemoveAt(i--);
                    Logger.Warn(
                        $"Failed to load plugin '{plugin.Attribute.Name}': a dependency is missing or failed.");
                    progressed = true;
                    continue;
                }

                if (dependencies.Any(dependency => !loadedNames.Contains(dependency)))
                    continue;

                remaining.RemoveAt(i--);
                if (Initialize(plugin)) {
                    loadedNames.Add(plugin.Attribute.Name);
                    count++;
                }
                else {
                    failedNames.Add(plugin.Attribute.Name);
                }
                progressed = true;
            }

            if (progressed)
                continue;

            foreach (PreparedPlugin plugin in remaining) {
                Discard(plugin);
                Logger.Warn(
                    $"Failed to load plugin '{plugin.Attribute.Name}': plugin dependencies contain a cycle.");
            }
            break;
        }

        return count;
    }

    private static void Discard(PreparedPlugin plugin) {
        plugin.Loader.Unload();
        string? directory = Path.GetDirectoryName(plugin.AssemblyPath);
        if (directory is null)
            return;

        try {
            Directory.Delete(directory, recursive: true);
        }
        catch (IOException) {
            return;
        }
        catch (UnauthorizedAccessException) {
            return;
        }
    }

    private static void CleanupStagedDirectories(string temporaryDirectory) {
        try {
            DateTime cutoff = DateTime.UtcNow - TimeSpan.FromHours(1);
            foreach (string pluginDirectory in Directory.GetDirectories(temporaryDirectory)) {
                foreach (string stagedDirectory in Directory.GetDirectories(pluginDirectory)) {
                    if (Directory.GetLastWriteTimeUtc(stagedDirectory) >= cutoff)
                        continue;

                    try {
                        Directory.Delete(stagedDirectory, recursive: true);
                    }
                    catch (IOException) {
                        continue;
                    }
                    catch (UnauthorizedAccessException) {
                        continue;
                    }
                }
            }
        }
        catch (Exception exception) {
            Logger.Warn($"Plugin stage cleanup failed: {exception.Message}");
        }
    }

    private void Disable(PluginContainer plugin) {
        if (plugin.State is PluginState.Disabled or PluginState.Failed)
            return;

        try {
            using (EnterRegistrationScope(plugin))
                plugin.Plugin.OnDisable();
            plugin.State = PluginState.Disabled;
        }
        catch (Exception exception) {
            plugin.State = PluginState.Failed;
            Logger.Warn($"Failed to disable plugin '{plugin.Description.Name}': {exception}");
        }

        _server.RemovePluginHandlers(plugin);
        _server.Commands.RemovePluginCommands(plugin);
        _server.RemovePluginRegistrations(plugin);
        CancelTasks(plugin);
        Unload(plugin);
    }

    private static void Unload(PluginContainer plugin) {
        string? stagedDirectory = Path.GetDirectoryName(plugin.AssemblyPath);
        plugin.Loader.Unload();
        DirectoryInfo? temporaryDirectory = stagedDirectory is null
            ? null
            : new DirectoryInfo(stagedDirectory).Parent?.Parent;
        if (temporaryDirectory?.Name != ".temp")
            return;

        try {
            Directory.Delete(stagedDirectory!, recursive: true);
        }
        catch (IOException) {
            return;
        }
        catch (UnauthorizedAccessException) {
            return;
        }
    }

    private static string StagePlugin(PluginCandidate candidate, string temporaryDirectory) {
        string stagedPluginDirectory = Path.Combine(
            temporaryDirectory,
            candidate.Name,
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagedPluginDirectory);
        CopyPluginFiles(Path.GetDirectoryName(candidate.AssemblyPath)!, stagedPluginDirectory);
        return Path.Combine(stagedPluginDirectory, Path.GetFileName(candidate.AssemblyPath));
    }

    private static HashSet<string> CreateSharedAssemblyNames() {
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        foreach (Assembly assembly in AssemblyLoadContext.Default.Assemblies) {
            string? name = assembly.GetName().Name;
            if (name is "Basalt" or "Binary" or "Tracy" or "Tmds.LibC" ||
                name?.StartsWith("Basalt.", StringComparison.Ordinal) == true) {
                names.Add(name);
            }
        }

        names.Add(typeof(Plugin).Assembly.GetName().Name!);
        names.Add(typeof(Server).Assembly.GetName().Name!);
        return names;
    }

    private static Type GetEntry(Assembly assembly, PluginAttribute attribute) {
        if (attribute.EntryTypeName is { Length: > 0 } entryTypeName) {
            Type? entryType = assembly.GetType(entryTypeName, throwOnError: false);
            if (entryType is null || entryType.IsAbstract || !typeof(Plugin).IsAssignableFrom(entryType))
                throw new InvalidOperationException(
                    $"Plugin entry '{entryTypeName}' is not a valid Plugin type in the loaded assembly.");

            return entryType;
        }

        Type[] entries = assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(Plugin).IsAssignableFrom(type))
            .ToArray();
        return entries.Length switch {
            0 => throw new InvalidOperationException("Plugin assembly does not contain a Plugin type."),
            1 => entries[0],
            _ => throw new InvalidOperationException("Plugin assembly contains multiple Plugin types.")
        };
    }

    private static void CopyPluginFiles(string sourceDirectory, string destinationDirectory) {
        foreach (string file in Directory.GetFiles(sourceDirectory, "*", SearchOption.TopDirectoryOnly)) {
            string fileName = Path.GetFileName(file);
            if (!fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                !fileName.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase) &&
                !fileName.EndsWith(".runtimeconfig.json", StringComparison.OrdinalIgnoreCase))
                continue;

            File.Copy(file, Path.Combine(destinationDirectory, fileName));
        }

        string resourcesDirectory = Path.Combine(sourceDirectory, "resources");
        if (!Directory.Exists(resourcesDirectory))
            return;

        foreach (string file in Directory.GetFiles(resourcesDirectory, "*", SearchOption.AllDirectories)) {
            string destination = Path.Combine(destinationDirectory, Path.GetRelativePath(sourceDirectory, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }

        string runtimesDirectory = Path.Combine(sourceDirectory, "runtimes");
        if (!Directory.Exists(runtimesDirectory))
            return;

        foreach (string file in Directory.GetFiles(runtimesDirectory, "*", SearchOption.AllDirectories)) {
            string destination = Path.Combine(destinationDirectory, Path.GetRelativePath(sourceDirectory, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
    }

    private void LogLoadFailure(string assemblyPath, Exception exception) {
        Logger.Warn($"Failed to load plugin '{Path.GetFileName(assemblyPath)}': {exception}");
        if (_server.Properties.CrashOnPluginLoadFailure)
            ExceptionDispatchInfo.Capture(exception).Throw();
    }

    private sealed record PluginCandidate(int Order, string Name, string AssemblyPath);

    private sealed record PreparedPlugin(
        int Order,
        string AssemblyPath,
        PluginAssemblyLoadContext Loader,
        PluginAttribute Attribute,
        Type Entry,
        double LoaderMilliseconds,
        double AssemblyMilliseconds,
        double ReflectionMilliseconds);

    private sealed class RegistrationScope : IDisposable {
        private readonly PluginManager _manager;
        private readonly PluginContainer? _previous;

        public RegistrationScope(PluginManager manager, PluginContainer? previous) {
            _manager = manager;
            _previous = previous;
        }

        public void Dispose() {
            _manager._registrationPlugin.Value = _previous;
        }
    }
}
