using System.Reflection;
using System.Reflection.PortableExecutable;
using BasaltBE.Plugins.Packaging;

namespace BasaltBE.Plugins;

internal static class PluginAssembly {
    internal static PackageDescriptor Inspect(string directory, PluginManifest manifest, string? runtime) {
        string assembly = Path.Combine(directory, manifest.Name + ".dll");
        if (!File.Exists(assembly))
            throw new InvalidDataException($"Plugin assembly must be named {manifest.Name}.dll. Set AssemblyName in the project.");

        string[] shared = PluginLoadContext.HostAssemblies;
        if (shared.Contains(manifest.Name + ".dll", StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("Plugin name conflicts with a host assembly.");

        Dictionary<string, string> assemblies = new(StringComparer.OrdinalIgnoreCase);
        foreach (string file in Directory.GetFiles(directory, "*.dll")) {
            using FileStream stream = File.OpenRead(file);
            using PEReader image = new(stream);
            if (!image.HasMetadata)
                continue;

            string identity = AssemblyName.GetAssemblyName(file).Name!;
            if (!assemblies.TryAdd(identity, file))
                throw new InvalidDataException($"Duplicate assembly identity: {identity}.");
        }
        string references = Path.Combine(directory, ".basalt", "references");
        if (Directory.Exists(references)) {
            foreach (string file in Directory.GetFiles(references, "*.dll"))
                assemblies.TryAdd(Path.GetFileNameWithoutExtension(file), file);
        }
        foreach (Assembly host in System.Runtime.Loader.AssemblyLoadContext.Default.Assemblies) {
            if (!host.IsDynamic && shared.Contains(host.GetName().Name + ".dll", StringComparer.OrdinalIgnoreCase))
                assemblies.TryAdd(host.GetName().Name!, host.Location);
        }
        foreach (string file in Directory.GetFiles(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "*.dll"))
            assemblies.TryAdd(Path.GetFileNameWithoutExtension(file), file);

        using MetadataLoadContext metadata = new(new PathAssemblyResolver(assemblies.Values));
        Assembly compiled = metadata.LoadFromAssemblyPath(assembly);
        if (compiled.GetName().Name != manifest.Name)
            throw new InvalidDataException("Plugin assembly identity must match the plugin name.");

        CustomAttributeData? framework = compiled.GetCustomAttributesData().FirstOrDefault(attribute =>
            attribute.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute");
        if (framework?.ConstructorArguments[0].Value as string != ".NETCoreApp,Version=v10.0")
            throw new InvalidDataException("Plugin project must target net10.0.");

        List<Type> entries = [];
        foreach (Type type in compiled.GetTypes()) {
            if (!type.IsPublic || type.IsAbstract || type.ContainsGenericParameters)
                continue;

            for (Type? parent = type.BaseType; parent is not null; parent = parent.BaseType) {
                if (parent.FullName != typeof(Plugin).FullName || parent.Assembly.GetName().Name != typeof(Plugin).Assembly.GetName().Name)
                    continue;

                entries.Add(type);
                break;
            }
        }
        if (entries.Count != 1 || entries[0].GetConstructor(Type.EmptyTypes) is null)
            throw new InvalidDataException("The plugin must have one public concrete Plugin type with a public parameterless constructor.");

        return new PackageDescriptor {
            EntryAssembly = manifest.Name + ".dll",
            EntryType = entries[0].FullName!,
            RuntimeIdentifier = runtime,
            HostAssemblies = shared
        };
    }
}
