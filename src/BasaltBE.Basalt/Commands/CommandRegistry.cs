using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BasaltBE.Basalt.Commands.Types;
using BasaltBE.Basalt.Entities;
using BasaltBE.Logging;
using BasaltBE.NetherNet.Types;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;

namespace BasaltBE.Basalt.Commands;

public sealed class CommandRegistry(Server server) {
    public const int MaximumCommandLength = 2048;
    private const int MaximumPendingCommands = 1024;
    private readonly Dictionary<string, Command> byName = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Command> definitions = [];
    private readonly ConcurrentQueue<QueuedCommand> pending = new();
    private int pendingCount;
    internal CommandCatalog Catalog = null!;
    private CommandCatalog publicCatalog = null!;

    public void Register(Command definition) {
        ArgumentNullException.ThrowIfNull(definition);
        if (Catalog is not null)
            throw new InvalidOperationException("Register commands before starting the server.");
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        foreach (string name in new[] { definition.Name }.Concat(definition.Aliases)) {
            if (name.Length == 0 || name.Length > 64 || name.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('_' or ':' or '-')) ||
                !names.Add(name) || byName.ContainsKey(name))
                throw new ArgumentException($"Invalid or duplicate command name '{name}'.", nameof(definition));
        }
        if (definition.Overloads.Count == 0)
            throw new ArgumentException("A command needs at least one overload.", nameof(definition));
        foreach (CommandOverload overload in definition.Overloads) {
            if (overload.Parameters.Length > 32)
                throw new ArgumentException("A command overload supports up to 32 parameters.", nameof(definition));
            bool optional = false;
            HashSet<string> parameterNames = new(StringComparer.Ordinal);
            foreach (CommandParameter parameter in overload.Parameters) {
                ArgumentNullException.ThrowIfNull(parameter.Parser);
                if (string.IsNullOrWhiteSpace(parameter.Name) || !parameterNames.Add(parameter.Name) || optional && !parameter.Optional)
                    throw new ArgumentException("Parameter names must be unique. Optional parameters must be last.", nameof(definition));
                optional |= parameter.Optional;
            }
        }
        definitions.Add(definition);
        foreach (string name in names)
            byName.Add(name, definition);
    }

    public CommandResult Execute(string input, Player? player = null) {
        return Execute(input, player is null ? new ServerCommandExecutor(server) : new PlayerCommandExecutor(player));
    }

    public CommandResult Execute(string input, CommandExecutor executor) {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(executor);
        if (!ReferenceEquals(executor.Server, server))
            throw new ArgumentException("The executor belongs to another server.", nameof(executor));
        if (input.Length > MaximumCommandLength || Encoding.UTF8.GetByteCount(input) > MaximumCommandLength)
            return new(false, "Command is too long.");
        foreach (char character in input) {
            if (char.IsControl(character))
                return new(false, "Command contains a control character.");
        }
        ReadOnlySpan<char> text = input.AsSpan().Trim();
        if (!text.IsEmpty && text[0] == '/')
            text = text[1..];

        CommandReader reader = new(text);
        Command? definition = ReadCommand(ref reader);
        if (definition is null)
            return new(false, "Unknown command.");

        if (!executor.Allows(definition.Permission))
            return new(false, "You do not have permission to use this command.");

        bool exclusive = server.Started && server.Properties.Ticking == "world" &&
            !definition.Concurrent && !server.Gameplay.IsWriteLockHeld;

        if (exclusive && server.Gameplay.IsReadLockHeld)
            return new(false, "Queue gameplay commands from tick listeners.");
        if (exclusive && !server.Gameplay.TryEnterWriteLock(0))
            return new(false, "Gameplay is busy. Please try the command again.");

        try {
            CommandContext context = new(executor);

            foreach (CommandOverload overload in definition.Overloads) {
                CommandReader arguments = reader;
                context.Parameters = overload.Parameters;
                context.Arguments = overload.Parameters.Length == 0 ? [] : new object?[overload.Parameters.Length];
                bool valid = true;

                for (int index = 0; index < overload.Parameters.Length; index++) {
                    CommandParameter parameter = overload.Parameters[index];
                    if (arguments.Complete && parameter.Optional)
                        break;

                    try {
                        if (!parameter.Parser.Parse(context, ref arguments, out context.Arguments[index])) {
                            valid = false;
                            break;
                        }
                    }
                    catch (JsonException) {
                        valid = false;
                        break;
                    }
                }

                if (valid && arguments.Complete)
                    return definition.Execute(context);
            }

            return new(false, $"Usage: {definition.GetUsage()}");
        }
        finally {
            if (exclusive)
                server.Gameplay.ExitWriteLock();
        }
    }

    internal void Prepare() {
        Catalog = new CommandCatalog(definitions);
        publicCatalog = new CommandCatalog(definitions.Where(command => command.Permission.Length == 0));
    }

    internal CommandCatalog GetCatalog(PlayerPermissions permissions) {
        if (permissions.Contains("basalt.operator"))
            return Catalog;

        List<Command> visible = [];
        foreach (Command command in definitions) {
            if (permissions.Allows(command.Permission))
                visible.Add(command);
        }
        if (visible.Count == definitions.Count)
            return Catalog;
        if (visible.Count == publicCatalog.Packet.Commands.Length)
            return publicCatalog;
        return new CommandCatalog(visible);
    }

    internal bool Enqueue(Player player, string command, CommandOriginData origin) {
        if (command.Length > MaximumCommandLength || Encoding.UTF8.GetByteCount(command) > MaximumCommandLength ||
            !string.Equals(origin.Type, "player", StringComparison.OrdinalIgnoreCase) || origin.RequestId.Length > 128)
            return false;

        if (Interlocked.Increment(ref player.PendingCommands) > 8) {
            Interlocked.Decrement(ref player.PendingCommands);
            return false;
        }
        if (Interlocked.Increment(ref pendingCount) > MaximumPendingCommands) {
            Interlocked.Decrement(ref pendingCount);
            Interlocked.Decrement(ref player.PendingCommands);
            return false;
        }

        pending.Enqueue(new QueuedCommand(new PlayerCommandExecutor(player), command, origin, null));
        return true;
    }

    /// <summary>Queues one command. Completion runs on the server thread after gameplay ownership is released.</summary>
    public bool Enqueue(string command, CommandExecutor executor, Action<CommandResult> completed) {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(completed);
        if (!ReferenceEquals(executor.Server, server) || command.Length > MaximumCommandLength)
            return false;

        if (Interlocked.Increment(ref pendingCount) > MaximumPendingCommands) {
            Interlocked.Decrement(ref pendingCount);
            return false;
        }

        pending.Enqueue(new QueuedCommand(executor, command, new CommandOriginData(), completed));
        return true;
    }

    internal void Tick() {
        int count = Math.Min(16, Volatile.Read(ref pendingCount));

        for (int index = 0; index < count && !server.Stopping && pending.TryDequeue(out QueuedCommand request); index++) {
            ReadOnlySpan<char> text = request.Command.AsSpan().Trim();
            if (!text.IsEmpty && text[0] == '/')
                text = text[1..];

            CommandReader reader = new(text);
            Command? command = ReadCommand(ref reader);

            bool exclusive = server.Started && server.Properties.Ticking == "world" && command?.Concurrent != true;
            bool admitted = !exclusive || server.Gameplay.TryEnterWriteLock(0);

            if (!admitted && Stopwatch.GetElapsedTime(request.Created).TotalSeconds < 5) {
                pending.Enqueue(request);
                continue;
            }

            Interlocked.Decrement(ref pendingCount);
            if (request.Completed is null && request.Executor.Player is { } queuedPlayer)
                Interlocked.Decrement(ref queuedPlayer.PendingCommands);

            CommandResult result;

            try {
                if (request.Completed is null && request.Executor.Player is { } player && (player.Disposed || !player.Spawned))
                    continue;

                result = admitted
                    ? Execute(request.Command, request.Executor)
                    : new(false, "Gameplay is busy. The command was not run.");
            }
            catch (Exception error) when (error is PeerDisconnectedException or ObjectDisposedException) {
                result = new(false, "A player disconnected while the command was running. Some changes may have been applied.");
            }
            catch (Exception error) {
                Logger.Scoped("Commands").Error($"Command execution failed (command: {request.Command}): {error}");
                result = new(false, "Command execution failed.");
            }
            finally {
                if (exclusive && admitted)
                    server.Gameplay.ExitWriteLock();
            }

            SendOutput(request, result);
        }
    }

    private Command? ReadCommand(ref CommandReader reader) {
        if (reader.Read(out ReadOnlySpan<char> name) &&
            byName.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(name, out Command? command))
            return command;

        return null;
    }

    private static void SendOutput(in QueuedCommand request, CommandResult result) {
        if (request.Completed is { } completed) {
            completed(result);
            return;
        }

        Player player = request.Executor.Player!;

        try {
            player.SendPacket(new CommandOutputPacket {
                Origin = request.Origin,
                Output = new CommandOutput {
                    Type = "alloutput",
                    SuccessCount = result.Success ? 1u : 0,
                    Messages = [new CommandOutputMessage {
                        Id = result.Message,
                        Successful = result.Success
                    }]
                }
            });
        }
        catch (Exception error) when (error is PeerDisconnectedException or ObjectDisposedException) {
        }
    }
}
