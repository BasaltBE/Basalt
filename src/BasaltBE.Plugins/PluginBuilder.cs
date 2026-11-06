using System.Diagnostics;
using System.Text.Json;
using BasaltBE.Plugins.Packaging;

namespace BasaltBE.Plugins;

public static class PluginBuilder {
    public static string Build(string? project = null, string? output = null, string? runtime = null,
        bool restore = true, CancellationToken cancellation = default) {
        if (project is null) {
            string[] projects = Directory.GetFiles(Environment.CurrentDirectory, "*.csproj");
            if (projects.Length != 1)
                throw new ArgumentException("Select one project with --project. Candidates: " + string.Join(", ", projects.Select(Path.GetFileName)));
            project = projects[0];
        }

        project = Path.GetFullPath(project);
        string directory = Path.GetDirectoryName(project)!;
        using FileStream manifestStream = File.OpenRead(Path.Combine(directory, "plugin.json"));
        PluginManifest manifest = PluginManifest.Read(manifestStream);
        string staging = Path.Combine(Path.GetTempPath(), "basalt-plugin-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try {
            cancellation.ThrowIfCancellationRequested();
            ProcessStartInfo start = new("dotnet") {
                UseShellExecute = false,
                WorkingDirectory = directory,
                ArgumentList = {
                    "publish", project,
                    "--configuration", "Release",
                    "--output", staging,
                    "--self-contained", "false",
                    "-p:UseAppHost=false",
                    "-p:PublishSingleFile=false",
                    "-p:PublishTrimmed=false",
                    "-p:EnableDynamicLoading=true",
                    "-p:DebugType=None",
                    "-p:DebugSymbols=false",
                    "-p:ContinuousIntegrationBuild=true"
                }
            };
            if (restore) {
                DirectoryInfo? repository = new(directory);
                while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "BasaltBE.sln")))
                    repository = repository.Parent;

                string[] sources = [
                    Path.Combine(AppContext.BaseDirectory, "packages"),
                    Path.Combine(Environment.CurrentDirectory, "packages"),
                    Path.Combine(repository?.FullName ?? Environment.CurrentDirectory, "artifacts", "packages")
                ];
                foreach (string source in sources) {
                    if (!Directory.Exists(source) || !Directory.EnumerateFiles(source, "BasaltBE.Plugins.*.nupkg").Any())
                        continue;

                    string escaped = source.Replace("%", "%25").Replace(";", "%3B").Replace(",", "%2C");
                    start.ArgumentList.Add($"-p:RestoreAdditionalProjectSources={escaped}");
                    break;
                }
            }
            else {
                start.ArgumentList.Add("--no-restore");
            }
            if (runtime is not null) {
                start.ArgumentList.Add("--runtime");
                start.ArgumentList.Add(runtime);
            }

            using Process process = Process.Start(start) ?? throw new IOException("Could not start the .NET SDK.");
            using CancellationTokenRegistration registration = cancellation.Register(() => {
                try {
                    if (!process.HasExited)
                        process.Kill(true);
                }
                catch (InvalidOperationException) { }
            });
            process.WaitForExit();
            cancellation.ThrowIfCancellationRequested();
            if (process.ExitCode != 0)
                throw new InvalidOperationException("Plugin build failed.");

            if (!File.Exists(Path.Combine(staging, "host.json")))
                throw new InvalidDataException("The project must reference BasaltBE.Plugins.");

            PackageDescriptor descriptor = PluginAssembly.Inspect(staging, manifest, runtime);
            File.WriteAllText(Path.Combine(staging, "package.json"), JsonSerializer.Serialize(descriptor, PluginManifest.Json));
            string[] hostAssemblies = descriptor.HostAssemblies;
            string contract = Path.Combine(staging, "host.json");
            string deps = Path.Combine(staging, manifest.Name + ".deps.json");
            using (JsonDocument graph = JsonDocument.Parse(File.ReadAllText(deps))) {
                foreach (JsonProperty target in graph.RootElement.GetProperty("targets").EnumerateObject()) {
                    foreach (JsonProperty library in target.Value.EnumerateObject()) {
                        if (runtime is null && (library.Value.TryGetProperty("native", out _) ||
                            library.Value.TryGetProperty("runtimeTargets", out JsonElement assets) &&
                            assets.EnumerateObject().Any(asset => asset.Value.TryGetProperty("assetType", out JsonElement kind) && kind.GetString() == "native")))
                            throw new InvalidDataException("Native dependencies require --runtime <rid>.");
                    }
                }
            }
            foreach (string host in hostAssemblies) {
                string file = Path.Combine(staging, host);
                if (File.Exists(file))
                    File.Delete(file);
            }
            File.Delete(contract);
            string references = Path.Combine(staging, ".basalt", "references");
            if (Directory.Exists(references))
                Directory.Delete(references, true);
            string metadataDirectory = Path.Combine(staging, ".basalt");
            if (Directory.Exists(metadataDirectory) && !Directory.EnumerateFileSystemEntries(metadataDirectory).Any())
                Directory.Delete(metadataDirectory);
            File.WriteAllText(Path.Combine(staging, "plugin.json"), JsonSerializer.Serialize(manifest, PluginManifest.Json));
            string destination = Path.Combine(Path.GetFullPath(output ?? Path.Combine(directory, "artifacts")), $"{manifest.Name}-{manifest.Version}.plugin");
            cancellation.ThrowIfCancellationRequested();
            PluginArchive.Write(staging, destination);
            return destination;
        }
        finally {
            Directory.Delete(staging, true);
        }
    }
}
