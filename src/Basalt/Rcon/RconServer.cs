namespace Basalt.Core.Rcon;

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Basalt.Core.Commands;

public sealed class RconServer {
    const int MaxLineLength = 8192;
    readonly Server _server;
    readonly string _password;
    readonly TcpListener _listener;
    readonly CancellationTokenSource _cancellation = new();
    Task? _acceptTask;

    public RconServer(Server server, ushort port, string password) {
        _server = server;
        _password = password;
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public void Start() {
        _listener.Start();
        _acceptTask = AcceptClients();
        Logger.Info($"RCON listening on 0.0.0.0:{((IPEndPoint)_listener.LocalEndpoint).Port}");
    }

    public void Stop() {
        _cancellation.Cancel();
        _listener.Stop();
        try {
            _acceptTask?.Wait(1000);
        }
        catch (AggregateException exception) when (exception.InnerExceptions.All(static e => e is OperationCanceledException)) { }
        _cancellation.Dispose();
    }

    async Task AcceptClients() {
        try {
            while (!_cancellation.IsCancellationRequested) {
                TcpClient client = await _listener.AcceptTcpClientAsync(_cancellation.Token);
                _ = Task.Run(() => HandleClient(client), _cancellation.Token);
            }
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (_cancellation.IsCancellationRequested) { }
        catch (SocketException) when (_cancellation.IsCancellationRequested) { }
        catch (Exception exception) {
            Logger.Warn($"RCON listener stopped: {exception.Message}");
        }
    }

    async Task HandleClient(TcpClient client) {
        using (client) {
            try {
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new(stream, Encoding.UTF8, false, 1024, true);
                using StreamWriter writer = new(stream, new UTF8Encoding(false), 1024, true) {
                    AutoFlush = true,
                    NewLine = "\n"
                };

                await writer.WriteLineAsync("BASALT-RCON 1");
                string? password = await reader.ReadLineAsync(_cancellation.Token);
                if (password is null || !PasswordsMatch(password)) {
                    await writer.WriteLineAsync("AUTH_FAILED");
                    return;
                }

                await writer.WriteLineAsync("OK");
                while (!_cancellation.IsCancellationRequested) {
                    string? command = await reader.ReadLineAsync(_cancellation.Token);
                    if (command is null) {
                        return;
                    }

                    command = command.Trim();
                    if (command.Length == 0) {
                        continue;
                    }

                    if (command.Equals("quit", StringComparison.OrdinalIgnoreCase)) {
                        await writer.WriteLineAsync("Closing Connection!");
                        return;
                    }

                    if (command.Length > MaxLineLength) {
                        await writer.WriteLineAsync("ERROR command is too long");
                        continue;
                    }

                    CommandResult result;
                    if (command.Equals("metrics", StringComparison.OrdinalIgnoreCase)) {
                        RconMetricsTask task = new(_server);
                        _server.Scheduler.Schedule(task);
                        result = await task.Completion.WaitAsync(_cancellation.Token);
                    }
                    else {
                        RconCommandTask task = new(_server, command);
                        _server.Scheduler.Schedule(task);
                        result = await task.Completion.WaitAsync(_cancellation.Token);
                    }
                    string response = result.Message ?? (result.Success ? "OK" : "ERROR");
                    await writer.WriteLineAsync(response.Replace('\r', ' ').Replace('\n', ' '));
                }
            }
            catch (OperationCanceledException) when (_cancellation.IsCancellationRequested) { }
            catch (IOException) { }
            catch (SocketException) { }
            catch (Exception exception) {
                Logger.Warn($"RCON client failed: {exception.Message}");
            }
        }
    }

    bool PasswordsMatch(string password) {
        byte[] expected = Encoding.UTF8.GetBytes(_password);
        byte[] actual = Encoding.UTF8.GetBytes(password);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
