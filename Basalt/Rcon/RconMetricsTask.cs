namespace Basalt.Core.Rcon;

using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Basalt.Core.Commands;
using Basalt.Core.Entities.Traits.Attribute;
using Basalt.Core.Tasks;

public sealed class RconMetricsTask : ServerTask {
    static long _lastCpuTimestamp;
    static TimeSpan _lastCpuTime;
    readonly Server _server;
    readonly TaskCompletionSource<CommandResult> _completion = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    public RconMetricsTask(Server server) {
        _server = server;
        MainThreadCompletion = false;
    }

    public Task<CommandResult> Completion => _completion.Task;

    public override void Execute() {
        try {
            using Process process = Process.GetCurrentProcess();
            long cpuTimestamp = Stopwatch.GetTimestamp();
            TimeSpan cpuTime = process.TotalProcessorTime;
            double cpuPercent;
            if (_lastCpuTimestamp == 0) {
                double elapsedMilliseconds = (DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalMilliseconds;
                cpuPercent = elapsedMilliseconds <= 0
                    ? 0
                    : cpuTime.TotalMilliseconds / elapsedMilliseconds / Environment.ProcessorCount * 100;
            }
            else {
                double elapsedMilliseconds = Stopwatch.GetElapsedTime(_lastCpuTimestamp, cpuTimestamp).TotalMilliseconds;
                cpuPercent = elapsedMilliseconds <= 0
                    ? 0
                    : (cpuTime - _lastCpuTime).TotalMilliseconds / elapsedMilliseconds / Environment.ProcessorCount * 100;
            }

            _lastCpuTimestamp = cpuTimestamp;
            _lastCpuTime = cpuTime;

        object[] players = _server.CurrentPlayersSnapshot
                .Select(player => (object)new {
                    name = player.Username,
                    ip = player.Connection is { } connection &&
                        connection.GetType().GetProperty("Endpoint")?.GetValue(connection) is EndPoint endpoint
                            ? endpoint is IPEndPoint ipEndpoint
                                ? ipEndpoint.Address.ToString()
                                : endpoint.ToString()
                            : null,
                    world = player.Dimension?.World?.Name,
                    dimension = player.Dimension?.Identifier,
                    gamemode = player.Gamemode.ToString(),
                    health = player.GetTrait<EntityHealthTrait>() is { } health
                        ? new {
                            current = Math.Round(health.CurrentValue, 2),
                            maximum = Math.Round(health.MaximumValue, 2)
                        }
                        : null
                })
                .ToArray();

            object[] worlds = _server.Worlds
                .Select(world => (object)new {
                    name = world.Name,
                    dimensions = world.Dimensions.Select(dimension => dimension.Identifier).ToArray()
                })
                .ToArray();

            object[] plugins = _server.Plugins.Plugins
                .Select(plugin => (object)new {
                    name = plugin.Description.Name,
                    version = plugin.Description.Version,
                    authors = plugin.Description.Authors,
                    state = plugin.State.ToString()
                })
                .ToArray();

            object[] resourcePacks = _server.ResourcePacks.Packs
                .Select(pack => (object)new {
                    name = pack.Name,
                    folder = pack.FolderName,
                    author = pack.Author,
                    version = pack.VersionString,
                    description = pack.Description,
                    uuid = pack.Uuid,
                    sizeMb = Math.Round(pack.Size / 1024.0 / 1024.0, 2),
                    status = "Loaded"
                })
                .ToArray();

            object metrics = new {
                tps = Math.Round(_server.Tps, 2),
                cpu = new {
                    processPercent = Math.Round(cpuPercent, 2),
                    logicalProcessors = Environment.ProcessorCount
                },
                memory = new {
                    managedMb = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 2),
                    workingSetMb = Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 2),
                    privateMb = Math.Round(process.PrivateMemorySize64 / 1024.0 / 1024.0, 2)
                },
                players,
                worlds,
                plugins,
                resourcePacks
            };

            _completion.TrySetResult(CommandResult.OkMessage(JsonSerializer.Serialize(metrics)));
        }
        catch (Exception exception) {
            _completion.TrySetResult(CommandResult.Error(exception.Message));
        }
    }
}
