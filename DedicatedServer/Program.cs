
using Basalt.Core;
using Basalt.Core.Commands;
using Basalt.Core.Profiling;

namespace Basalt.DedicatedServer;

sealed class Program {
    static void Main() {
        // Profiler.Enabled = true;

        Logger.Init();
        const string serverPropertiesPath = "server.properties";
        ServerProperties props = ServerProperties.LoadFromPath(serverPropertiesPath);
        props.ApplyMetadata<Properties>();
        props.KeepOnlyMetadata();

        EnsurePropertyDefaults(props);
        props.SaveToPath(serverPropertiesPath);

        Properties properties = props.Parse<Properties>();
        Core.Server server = new(properties);
        using ManualResetEventSlim shutdown = new(false);
        using CancellationTokenSource consoleCancellation = new();

        Console.CancelKeyPress += (_, eventArgs) => {
            eventArgs.Cancel = true;
            shutdown.Set();
        };

        server.Start();
        ConsoleInterface.Start(server, consoleCancellation.Token, shutdown.Set);
        shutdown.Wait();
        consoleCancellation.Cancel();
        server.Stop();
    }

    private static void EnsurePropertyDefaults(ServerProperties props) {
        if (!props.HasProperty("max-players")) props.SetNumericalProperty("max-players", 10);
        if (!props.HasProperty("online-mode")) props.SetBoolProperty("online-mode", true);
        if (!props.HasProperty("server-port")) props.SetNumericalProperty("server-port", 19132);
        if (!props.HasProperty("raknet-mtu")) props.SetNumericalProperty("raknet-mtu", 1024);
        if (!props.HasProperty("default-world")) props.SetStringProperty("default-world", "world");
        if (!props.HasProperty("world-provider")) props.SetStringProperty("world-provider", "leveldb");
        if (!props.HasProperty("world-path")) props.SetStringProperty("world-path", Path.Combine("worlds", props.GetStringProperty("default-world", "world") ?? "world"));
        if (!props.HasProperty("plugins-directory")) props.SetStringProperty("plugins-directory", "plugins");
        if (!props.HasProperty("compression-threshold")) props.SetNumericalProperty("compression-threshold", 1);
        if (!props.HasProperty("compression-algorithm")) props.SetStringProperty("compression-algorithm", "zlib");
        if (!props.HasProperty("max-view-distance")) props.SetNumericalProperty("max-view-distance", 32);
        if (!props.HasProperty("simulation-distance")) props.SetNumericalProperty("simulation-distance", 4);
        if (!props.HasProperty("chunks-per-tick")) props.SetNumericalProperty("chunks-per-tick", 64);
    }
}
