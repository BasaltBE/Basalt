using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Basalt.Core.Nethernet;

public sealed class NetherNetSignalingServer : IDisposable {
    private readonly ushort _port;
    private readonly Func<string, string, CancellationToken, Task<string?>> _createAnswer;
    private CancellationTokenSource? _cancellation;
    private Task? _loop;
    private WebApplication? _application;

    public NetherNetSignalingServer(
        ushort port,
        Func<string, string, CancellationToken, Task<string?>> createAnswer) {
        if (port == 0) {
            throw new ArgumentOutOfRangeException(nameof(port));
        }

        _port = port;
        _createAnswer = createAnswer ?? throw new ArgumentNullException(nameof(createAnswer));
    }

    public void Start(CancellationToken cancellationToken = default) {
        if (_loop is not null) {
            throw new InvalidOperationException("The signaling server is already running.");
        }

        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(_port));
        WebApplication application = builder.Build();
        application.MapGet("/v1/join", static () => Results.NoContent());
        application.MapPost("/v1/join/{networkId}", Handle);
        _application = application;
        _loop = application.RunAsync(_cancellation.Token);
    }

    public async Task StopAsync() {
        if (_loop is null) {
            return;
        }

        _cancellation!.Cancel();
        await _application!.StopAsync().ConfigureAwait(false);
        await _loop.ConfigureAwait(false);
        _loop = null;
        await _application.DisposeAsync().ConfigureAwait(false);
        _application = null;
        _cancellation.Dispose();
        _cancellation = null;
    }

    public void Dispose() {
        StopAsync().GetAwaiter().GetResult();
    }

    private async Task<IResult> Handle(HttpRequest request, HttpResponse response, string networkId) {
        CancellationToken cancellationToken = request.HttpContext.RequestAborted;
        try {
            if (networkId.Length == 0 ||
                !request.ContentType?.StartsWith("application/sdp", StringComparison.OrdinalIgnoreCase) == true) {
                return Results.BadRequest();
            }

            using StreamReader reader = new(request.Body);
            string offer = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            string? answer = await _createAnswer(networkId, offer, cancellationToken).ConfigureAwait(false);
            if (answer is null) {
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            return Results.Text(answer, "application/sdp");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            return Results.Empty;
        }
        catch (Exception) {
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
