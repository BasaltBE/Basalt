namespace Basalt.Tests;

using System.Reflection;
using System.Runtime.Loader;
using Basalt.Core.Plugins;

public sealed class PluginAssemblyLoadContextTests {
    [Fact]
    public void SharedAssembliesUseTheDefaultContext() {
        string assemblyPath = typeof(Plugin).Assembly.Location;
        string assemblyName = typeof(Plugin).Assembly.GetName().Name!;
        PluginAssemblyLoadContext context = new(assemblyPath, new HashSet<string> {
            assemblyName
        });

        Assembly loaded = context.LoadFromAssemblyName(typeof(Plugin).Assembly.GetName());

        Assert.Same(typeof(Plugin).Assembly, loaded);
        context.Unload();
    }

    [Fact]
    public void SeparateContextsIsolateAssemblies() {
        string assemblyPath = typeof(Plugin).Assembly.Location;
        PluginAssemblyLoadContext firstContext = new(assemblyPath, new HashSet<string>());
        PluginAssemblyLoadContext secondContext = new(assemblyPath, new HashSet<string>());

        Assembly first = firstContext.LoadFromAssemblyPath(assemblyPath);
        Assembly second = secondContext.LoadFromAssemblyPath(assemblyPath);

        Assert.NotSame(first, second);
        Assert.NotSame(
            AssemblyLoadContext.GetLoadContext(first),
            AssemblyLoadContext.GetLoadContext(second));

        firstContext.Unload();
        secondContext.Unload();
    }

    [Fact]
    public void ExternalManagedDependenciesLoadIntoThePluginContext() {
        string assemblyPath = typeof(PluginAssemblyLoadContextTests).Assembly.Location;
        AssemblyName dependencyName = typeof(Assert).Assembly.GetName();
        PluginAssemblyLoadContext context = new(assemblyPath, new HashSet<string>());

        Assembly dependency = context.LoadFromAssemblyName(dependencyName);

        Assert.Equal(dependencyName.Name, dependency.GetName().Name);
        Assert.NotSame(typeof(Assert).Assembly, dependency);
        context.Unload();
    }

}
