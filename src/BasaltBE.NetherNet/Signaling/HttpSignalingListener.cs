using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using BasaltBE.NetherNet.Connections;
using BasaltBE.NetherNet.Types;
using BasaltBE.Logging;
using SIPSorcery.Net;

namespace BasaltBE.NetherNet.Signaling;

internal sealed class HttpSignalingListener : IAsyncDisposable {
    private static readonly ScopedLogger Log = Logger.Scoped("Signaling");
    private readonly NethernetServerOptions options;
    private readonly IServerIdentity identity;
    private readonly Func<NethernetPeer, NethernetConnection, ValueTask> accepted;
    private readonly TcpListener listener;
    private readonly SemaphoreSlim capacity;
    private readonly Channel<TcpClient> clients;
    private readonly CancellationTokenSource cancellation = new();
    private readonly Task acceptTask;
    private readonly Task[] workers;

    public HttpSignalingListener(NethernetServerOptions options, IServerIdentity identity, Func<NethernetPeer, NethernetConnection, ValueTask> accepted) {
        this.options = options;
        this.identity = identity;
        this.accepted = accepted;
        listener = new TcpListener(options.HttpAddress);
        listener.Start(options.MaximumPeers);
        capacity = new SemaphoreSlim(options.MaximumPeers, options.MaximumPeers);
        clients = Channel.CreateBounded<TcpClient>(new BoundedChannelOptions(options.MaximumPeers) {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = true
        });
        workers = [
            .. Enumerable.Range(0, Math.Min(Environment.ProcessorCount, 8))
                .Select(_ => ProcessAsync(cancellation.Token))
        ];
        acceptTask = AcceptAsync(cancellation.Token);
    }

    public async ValueTask DisposeAsync() {
        cancellation.Cancel();
        listener.Stop();
        clients.Writer.TryComplete();
        try { await Task.WhenAll(workers.Append(acceptTask)).ConfigureAwait(false); }
        catch (Exception error) when (error is OperationCanceledException or SocketException or ObjectDisposedException) { }
        while (clients.Reader.TryRead(out TcpClient? client)) {
            client.Dispose();
            capacity.Release();
        }
        cancellation.Dispose();
        capacity.Dispose();
    }

    private async Task AcceptAsync(CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            TcpClient client;
            try { client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (SocketException) when (cancellationToken.IsCancellationRequested) { break; }
            if (!capacity.Wait(0, cancellationToken)) {
                client.Dispose();
                continue;
            }
            client.NoDelay = true;
            try { await clients.Writer.WriteAsync(client, cancellationToken).ConfigureAwait(false); }
            catch { client.Dispose(); capacity.Release(); throw; }
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken) {
        await foreach (TcpClient client in clients.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false)) {
            using (client) {
                try { await HandleAsync(client, cancellationToken).ConfigureAwait(false); }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return; }
                catch (OperationCanceledException) { }
                catch (IOException) { }
                catch (SocketException) { }
                catch (InvalidDataException) { }
                catch (Exception error) { Log.Error($"NetherNet signaling request failed: {error.Message}"); }
                finally { capacity.Release(); }
            }
        }
    }

    private async Task HandleAsync(TcpClient client, CancellationToken cancellationToken) {
        using CancellationTokenSource requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        requestTimeout.CancelAfter(options.RequestTimeout);
        CancellationToken requestToken = requestTimeout.Token;
        using NetworkStream networkStream = client.GetStream();
        Stream stream = networkStream;
        using SslStream? secureStream = options.TlsCertificate is null ? null : new SslStream(networkStream, leaveInnerStreamOpen: true);
        if (secureStream is not null) {
            await secureStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions {
                ServerCertificate = options.TlsCertificate,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                ClientCertificateRequired = false
            }, requestToken).ConfigureAwait(false);
            stream = secureStream;
        }
        byte[] header = GC.AllocateUninitializedArray<byte>(20 * 1024);
        (int headerLength, int bufferedLength) = await ReadHeadersAsync(stream, header, requestToken).ConfigureAwait(false);
        string[] lines = Encoding.ASCII.GetString(header, 0, headerLength - 4).Split("\r\n", StringSplitOptions.None);
        string[] request = lines[0].Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (request.Length != 3 || !request[2].StartsWith("HTTP/1.", StringComparison.Ordinal)) {
            await RespondAsync(stream, 400, "text/plain", "Invalid HTTP request", requestToken).ConfigureAwait(false);
            return;
        }
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
        foreach (string line in lines.Skip(1)) {
            int colon = line.IndexOf(':');
            if (colon <= 0) continue;
            headers[line[..colon].Trim()] = line[(colon + 1)..].Trim();
        }
        string path = request[1].Split('?', 2)[0];
        if (request[0] == "GET" && path == "/v1/join") {
            byte[] metadata = JsonSerializer.SerializeToUtf8Bytes(new {
                name = options.Advertisement.Name,
                protocol = options.Advertisement.Protocol,
                version = options.Advertisement.Version,
                level = options.Advertisement.Level,
                players = options.Advertisement.Players,
                maxPlayers = options.Advertisement.MaxPlayers,
                gameType = options.Advertisement.GameType
            });
            await RespondAsync(stream, 200, "application/json", metadata, requestToken).ConfigureAwait(false);
            return;
        }
        const string prefix = "/v1/join/";
        if (request[0] != "POST" || !path.StartsWith(prefix, StringComparison.Ordinal) ||
            path.Length == prefix.Length || path.AsSpan(prefix.Length).Contains('/')) {
            await RespondAsync(stream, 404, "text/plain", "Not found", requestToken).ConfigureAwait(false);
            return;
        }
        if (!headers.TryGetValue("content-type", out string? contentType) ||
            !contentType.Split(';', 2)[0].Trim().Equals("application/sdp", StringComparison.OrdinalIgnoreCase)) {
            await RespondAsync(stream, 400, "text/plain", "Expected application/sdp", requestToken).ConfigureAwait(false);
            return;
        }
        if (!headers.TryGetValue("content-length", out string? lengthText) ||
            !int.TryParse(lengthText, NumberStyles.None, CultureInfo.InvariantCulture, out int length) || length < 1) {
            await RespondAsync(stream, 400, "text/plain", "Content-Length is required", requestToken).ConfigureAwait(false);
            return;
        }

        if (length > options.MaximumSdpLength) {
            await RespondAsync(stream, 413, "text/plain", "SDP offer is too large", requestToken).ConfigureAwait(false);
            return;
        }
        byte[] offerBytes = GC.AllocateUninitializedArray<byte>(length);
        int bufferedBody = Math.Min(length, bufferedLength - headerLength);
        header.AsSpan(headerLength, bufferedBody).CopyTo(offerBytes);
        await stream.ReadExactlyAsync(offerBytes.AsMemory(bufferedBody), requestToken).ConfigureAwait(false);
        string offer = Encoding.UTF8.GetString(offerBytes);
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(requestToken);
        timeout.CancelAfter(options.NegotiationTimeout);

        NethernetConnection? connection = null;
        try {
            (connection, string answer) = await CreateAnswerAsync(offer, timeout.Token).ConfigureAwait(false);
            string networkIdText = Uri.UnescapeDataString(path[prefix.Length..]);
            if (!ulong.TryParse(networkIdText, NumberStyles.None, CultureInfo.InvariantCulture, out ulong networkId))
                throw new InvalidDataException("Network ID must be a uint64.");
            IPAddress? address = (client.Client.RemoteEndPoint as IPEndPoint)?.Address;
            NethernetPeer peer = new(networkId, RandomConnectionId(), address);
            connection.SetPeer(peer);
            await accepted(peer, connection).ConfigureAwait(false);
            byte[] body = Encoding.UTF8.GetBytes(answer);
            await RespondAsync(stream, 200, "application/sdp", body, requestToken).ConfigureAwait(false);
            connection = null;
        }
        catch (OperationCanceledException) when (!requestToken.IsCancellationRequested) {
            await RespondAsync(stream, 502, "text/plain", "Timed out during peer negotiation", requestToken).ConfigureAwait(false);
        }
        catch (Exception error) when (error is InvalidDataException or InvalidOperationException or CryptographicException) {
            await RespondAsync(stream, 400, "text/plain", "Could not process SDP offer", cancellationToken).ConfigureAwait(false);
        }
        finally {
            if (connection is not null) await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async ValueTask<(NethernetConnection Connection, string Answer)> CreateAnswerAsync(string offer, CancellationToken cancellationToken) {
        RTCConfiguration configuration = new() { iceServers = [] };
        RTCPeerConnection peer = new(configuration);
        NethernetConnection connection = new(peer, options);
        try {
            SetDescriptionResultEnum remote = peer.setRemoteDescription(new RTCSessionDescriptionInit { type = RTCSdpType.offer, sdp = offer });
            if (remote != SetDescriptionResultEnum.OK) throw new InvalidDataException("The SDP offer was rejected.");
            RTCSessionDescriptionInit answer = peer.createAnswer(null);
            await peer.setLocalDescription(answer).ConfigureAwait(false);
            await WaitForIceAsync(peer, cancellationToken).ConfigureAwait(false);
            string sdp = peer.localDescription?.sdp?.ToString() ?? answer.sdp;
            string signed = identity.AddAssertion(sdp);
            return (connection, signed);
        }
        catch {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private static async Task WaitForIceAsync(RTCPeerConnection peer, CancellationToken cancellationToken) {
        if (peer.iceGatheringState == RTCIceGatheringState.complete) return;
        TaskCompletionSource<bool> gathered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        void StateChanged(RTCIceGatheringState state) {
            if (state == RTCIceGatheringState.complete) gathered.TrySetResult(true);
        }
        peer.onicegatheringstatechange += StateChanged;
        try {
            if (peer.iceGatheringState == RTCIceGatheringState.complete) return;
            await gathered.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally { peer.onicegatheringstatechange -= StateChanged; }
    }

    private static async ValueTask<(int HeaderLength, int BufferedLength)> ReadHeadersAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken) {
        int length = 0;
        while (length < 16 * 1024 + 4096) {
            int read = await stream.ReadAsync(buffer.AsMemory(length, Math.Min(4096, buffer.Length - length)), cancellationToken).ConfigureAwait(false);
            if (read == 0) throw new IOException("HTTP request ended before headers completed.");
            length += read;
            int terminator = buffer.AsSpan(0, length).IndexOf("\r\n\r\n"u8);
            if (terminator >= 0) {
                int headerLength = terminator + 4;
                if (headerLength > 16 * 1024) throw new InvalidDataException("HTTP headers exceed the size limit.");
                return (headerLength, length);
            }
        }
        throw new InvalidDataException("HTTP headers exceed the size limit.");
    }

    private static async ValueTask RespondAsync(Stream stream, int status, string contentType, string body, CancellationToken cancellationToken) =>
        await RespondAsync(stream, status, contentType, Encoding.UTF8.GetBytes(body), cancellationToken).ConfigureAwait(false);

    private static async ValueTask RespondAsync(Stream stream, int status, string contentType, byte[] body, CancellationToken cancellationToken) {
        string reason = status switch { 200 => "OK", 400 => "Bad Request", 404 => "Not Found", 413 => "Payload Too Large", 502 => "Bad Gateway", _ => "Internal Server Error" };
        byte[] header = Encoding.ASCII.GetBytes($"HTTP/1.1 {status} {reason}\r\nContent-Type: {contentType}\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(body, cancellationToken).ConfigureAwait(false);
    }

    private static ulong RandomConnectionId() {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        RandomNumberGenerator.Fill(bytes);
        return System.Buffers.Binary.BinaryPrimitives.ReadUInt64LittleEndian(bytes);
    }
}

