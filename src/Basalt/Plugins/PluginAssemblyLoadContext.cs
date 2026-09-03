namespace Basalt.Core.Plugins;

using System.Reflection;
using System.Runtime.Loader;

internal sealed class PluginAssemblyLoadContext : AssemblyLoadContext {
    private readonly AssemblyDependencyResolver _resolver;
    private readonly IReadOnlySet<string> _sharedAssemblyNames;

    public PluginAssemblyLoadContext(
        string assemblyPath,
        IReadOnlySet<string> sharedAssemblyNames)
        : base(Path.GetFileNameWithoutExtension(assemblyPath), isCollectible: true) {
        _resolver = new AssemblyDependencyResolver(assemblyPath);
        _sharedAssemblyNames = sharedAssemblyNames;
    }

    protected override Assembly? Load(AssemblyName assemblyName) {
        if (_sharedAssemblyNames.Contains(assemblyName.Name!))
            return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);

        string? path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName) {
        string? path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
    }
}
