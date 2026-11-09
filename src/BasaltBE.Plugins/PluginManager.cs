using System.Reflection;
using System.Runtime.InteropServices;
using BasaltBE.Basalt;
using BasaltBE.Basalt.Events.Server;
using BasaltBE.Logging;
using BasaltBE.Plugins.Packaging;

namespace BasaltBE.Plugins;

public sealed class PluginManager {
    public const int Api = 1;
    public bool CompileSource;

    private static readonly ScopedLogger Log = Logger.Scoped("Plugins");
    private readonly Server server;
    private readonly string directory;
    private readonly List<(Plugin Plugin, PluginLoadContext Context)> loaded = [];
    private readonly HashSet<string> identifiers = new(StringComparer.OrdinalIgnoreCase);
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly IDisposable stopping;
    private bool enabled;
    private bool stopped;

    public PluginManager(Server server, string directory = "plugins") {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        this.server = server;
        this.directory = Path.GetFullPath(directory);
        Directory.CreateDirectory(this.directory);
        stopping = server.Events.Subscribe<ServerStoppingEvent>(_ => Disable());
    }

    public void LoadAll(CancellationToken cancellation = default) {
        if (Environment.CurrentManagedThreadId != ownerThread || enabled || stopped)
            throw new InvalidOperationException("Load plugins on the startup thread before enabling them.");

        foreach (string path in Directory.EnumerateFileSystemEntries(directory).Order(StringComparer.Ordinal)) {
            string name = Path.GetFileName(path);
            if (name.StartsWith('.') || name.Equals("data", StringComparison.OrdinalIgnoreCase)) continue;
            if (Directory.Exists(path) || path.EndsWith(".plugin", StringComparison.OrdinalIgnoreCase)) Load(path, cancellation);
        }
    }

    public void Load(string path, CancellationToken cancellation = default) {
        if (Environment.CurrentManagedThreadId != ownerThread || enabled || stopped)
            throw new InvalidOperationException("Load plugins on the startup thread before enabling them.");

        PluginLoadContext? context = null;
        Plugin? instance = null;
        try {
            cancellation.ThrowIfCancellationRequested();
            path = Path.GetFullPath(path);
            bool source = path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
            if (Directory.Exists(path)) {
                string[] projects = Directory.GetFiles(path, "*.csproj");
                if (projects.Length > 1) throw new InvalidDataException("Plugin folder contains more than one project.");
                if (projects.Length == 1) {
                    path = projects[0];
                    source = true;
                }
            }

            string folder = Directory.Exists(path) ? path : Path.GetDirectoryName(path)!;
            PluginArchive? archive = path.EndsWith(".plugin", StringComparison.OrdinalIgnoreCase) ? new(path) : null;
            PluginManifest manifest;
            if (archive is not null) {
                manifest = archive.Manifest;
            } else {
                using FileStream input = File.OpenRead(Path.Combine(folder, "plugin.json"));
                manifest = PluginManifest.Read(input);
            }

            string? mismatch = manifest.ApiMismatch(Api);
            if (mismatch is not null) {
                Log.Warn(mismatch);
                return;
            }
            if (identifiers.Contains(manifest.Id)) throw new InvalidDataException($"Plugin id is already loaded: {manifest.Id}.");

            if (source) {
                string output = Path.Combine(directory, ".cache", "source", manifest.Id);
                string archivePath = Path.Combine(output, $"{manifest.Name}-{manifest.Version}.plugin");
                if (CompileSource) {
                    archivePath = PluginBuilder.Build(path, output, RuntimeInformation.RuntimeIdentifier, cancellation: cancellation);
                } else if (!File.Exists(archivePath)) {
                    Log.Warn($"Skipped {manifest.Name}: no compiled plugin. Start with --roslyn to build it.");
                    return;
                }
                archive = new PluginArchive(archivePath);
                if (archive.Manifest.Id != manifest.Id || archive.Manifest.Api != manifest.Api || archive.Manifest.Name != manifest.Name)
                    throw new InvalidDataException("Compiled plugin identity does not match its source manifest.");
                manifest = archive.Manifest;
            }

            PackageDescriptor descriptor;
            string assembly;
            if (archive is not null) {
                descriptor = archive.Descriptor;
                if (descriptor.RuntimeIdentifier is not null && descriptor.RuntimeIdentifier != RuntimeInformation.RuntimeIdentifier)
                    throw new InvalidDataException($"Plugin runtime mismatch (got: {descriptor.RuntimeIdentifier}, expected: {RuntimeInformation.RuntimeIdentifier}).");

                string cache = archive.Extract(Path.Combine(directory, ".cache"));
                assembly = Path.Combine(cache, descriptor.EntryAssembly);
            } else {
                assembly = Path.Combine(folder, manifest.Name + ".dll");
                if (!Directory.Exists(path) && !string.Equals(assembly, path, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"Plugin assembly must be named {manifest.Name}.dll.");
                descriptor = PluginAssembly.Inspect(folder, manifest, null);
            }

            context = new PluginLoadContext(assembly);
            Assembly compiled = context.LoadFromAssemblyPath(assembly);
            Type type = compiled.GetType(descriptor.EntryType, true)!;
            if (!typeof(Plugin).IsAssignableFrom(type) || type.IsAbstract || type.ContainsGenericParameters || !type.IsPublic)
                throw new InvalidDataException("Invalid plugin entry type.");

            instance = (Plugin)Activator.CreateInstance(type)!;
            instance.Server = server;
            instance.Logger = Logger.Scoped(manifest.Name);
            instance.DataDirectory = Path.Combine(directory, "data", manifest.Id);
            instance.Manifest = manifest;
            Directory.CreateDirectory(instance.DataDirectory);
            instance.OnLoad();
            loaded.Add((instance, context));
            identifiers.Add(manifest.Id);
            Log.Info($"Loaded {manifest.Name} {manifest.Version}.");
        } catch (Exception error) {
            if (instance is not null) {
                try {
                    instance.OnDisable();
                } catch (Exception cleanup) {
                    Log.Error($"Could not disable {instance.Manifest.Name}: {cleanup.Message}");
                }
            }
            context?.Unload();
            if (error is OperationCanceledException && cancellation.IsCancellationRequested) throw;

            Exception reason = error is TargetInvocationException invocation ? invocation.InnerException ?? error : error;
            Log.Error($"Could not load {Path.GetFileName(path)}: {reason.Message}");
        }
    }

    public void Enable() {
        if (Environment.CurrentManagedThreadId != ownerThread || enabled || stopped)
            throw new InvalidOperationException("Enable plugins once on the startup thread.");

        enabled = true;
        for (int index = 0; index < loaded.Count; index++) {
            (Plugin plugin, PluginLoadContext context) = loaded[index];
            try {
                plugin.OnEnable();
            } catch (Exception error) {
                Log.Error($"Could not enable {plugin.Manifest.Name}: {error.Message}");
                try {
                    plugin.OnDisable();
                } catch (Exception cleanup) {
                    Log.Error($"Could not disable {plugin.Manifest.Name}: {cleanup.Message}");
                }
                identifiers.Remove(plugin.Manifest.Id);
                loaded.RemoveAt(index--);
                context.Unload();
            }
        }
    }

    private void Disable() {
        if (stopped) return;

        stopped = true;
        for (int index = loaded.Count - 1; index >= 0; index--) {
            (Plugin plugin, PluginLoadContext context) = loaded[index];
            try {
                plugin.OnDisable();
            } catch (Exception error) {
                Log.Error($"Could not disable {plugin.Manifest.Name}: {error.Message}");
            } finally {
                context.Unload();
            }
        }
        loaded.Clear();
        identifiers.Clear();
        stopping.Dispose();
    }
}
