using System.Reflection;
using System.Runtime.Loader;

namespace BasaltBE.Plugins;

internal sealed class PluginLoadContext : AssemblyLoadContext {
    internal static readonly string[] HostAssemblies = [
        "BasaltBE.Plugins.dll", "BasaltBE.Basalt.dll", "BasaltBE.NetherNet.dll",
        "BasaltBE.Protocol.dll", "BasaltBE.Nbt.dll", "BasaltBE.Logger.dll", "BasaltBE.Data.dll", "Binary.dll"
    ];

    private static readonly HashSet<string> FrameworkAssemblies = new(
        Directory.GetFiles(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "*.dll")
            .Select(file => Path.GetFileNameWithoutExtension(file)), StringComparer.OrdinalIgnoreCase);

    private readonly AssemblyDependencyResolver resolver;
    private readonly HashSet<string> shared;
    private readonly string directory;

    internal PluginLoadContext(string assembly) : base(Path.GetFileNameWithoutExtension(assembly), true) {
        resolver = new AssemblyDependencyResolver(assembly);
        directory = Path.GetDirectoryName(assembly)!;
        shared = new HashSet<string>(HostAssemblies.Select(file => Path.GetFileNameWithoutExtension(file)), StringComparer.OrdinalIgnoreCase);
    }

    protected override Assembly? Load(AssemblyName name) {
        if (name.Name is null) return null;
        if (shared.Contains(name.Name)) return Default.LoadFromAssemblyName(name);

        if (FrameworkAssemblies.Contains(name.Name)) return null;

        string? path = resolver.ResolveAssemblyToPath(name);
        if (path is not null) return LoadFromAssemblyPath(path);

        string sibling = Path.Combine(directory, name.Name + ".dll");
        if (File.Exists(sibling)) return LoadFromAssemblyPath(sibling);
        throw new FileNotFoundException($"Plugin dependency is missing: {name.Name}.");
    }

    protected override nint LoadUnmanagedDll(string name) {
        string? path = resolver.ResolveUnmanagedDllToPath(name);
        if (path is not null) return LoadUnmanagedDllFromPath(path);

        string extension = OperatingSystem.IsWindows() ? ".dll" : OperatingSystem.IsMacOS() ? ".dylib" : ".so";
        string[] candidates = OperatingSystem.IsWindows()
            ? [name, name + extension]
            : [name, name + extension, "lib" + name, "lib" + name + extension];
        foreach (string candidate in candidates) {
            string sibling = Path.Combine(directory, candidate);
            if (File.Exists(sibling)) return LoadUnmanagedDllFromPath(sibling);
        }

        return 0;
    }
}
