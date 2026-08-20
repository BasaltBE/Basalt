using System.Net.Http.Headers;
using System.Text;

namespace Basalt.Core.Nethernet;

public sealed class NetherNetHttpClient {
    private readonly HttpClient _httpClient;

    public NetherNetHttpClient(HttpClient? httpClient = null) {
        _httpClient = httpClient ?? new HttpClient();
    }

    public async Task<NetherNetPeer> ConnectAsync(
        Uri server,
        string networkId,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentException.ThrowIfNullOrWhiteSpace(networkId);

        NetherNetPeer peer = new();
        string offer = await peer.CreateOfferAsync(cancellationToken).ConfigureAwait(false);
        Uri endpoint = new(server, $"v1/join/{Uri.EscapeDataString(networkId)}");
        using StringContent content = new(offer, Encoding.UTF8, "application/sdp");
        using HttpResponseMessage response = await _httpClient
            .PostAsync(endpoint, content, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentType is not MediaTypeHeaderValue contentType ||
            contentType.MediaType?.Equals("application/sdp", StringComparison.OrdinalIgnoreCase) != true) {
            peer.Dispose();
            throw new InvalidDataException("The NetherNet signaling response was not SDP.");
        }

        string answer = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        peer.AcceptAnswer(answer);
        return peer;
    }
}
