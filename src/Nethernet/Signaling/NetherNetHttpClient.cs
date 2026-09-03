using System.Net.Http.Headers;
using System.Text;

namespace Basalt.Core.Nethernet;

public sealed class NetherNetHttpClient {
    private readonly HttpClient _httpClient;
    private readonly ClientIdentity? _clientIdentity;

    public NetherNetHttpClient(HttpClient? httpClient = null, ClientIdentity? clientIdentity = null) {
        _httpClient = httpClient ?? new HttpClient();
        _clientIdentity = clientIdentity;
    }

    public async Task<NetherNetPeer> ConnectAsync(
        Uri server,
        string networkId,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentException.ThrowIfNullOrWhiteSpace(networkId);
        if (server.Scheme != Uri.UriSchemeHttp && server.Scheme != Uri.UriSchemeHttps) {
            throw new ArgumentException("The NetherNet signaling server must use HTTP or HTTPS.", nameof(server));
        }

        Uri signalingEndpoint = new(server.AbsoluteUri.TrimEnd('/') + "/v1/join");
        using HttpResponseMessage capability = await _httpClient
            .GetAsync(signalingEndpoint, cancellationToken)
            .ConfigureAwait(false);
        capability.EnsureSuccessStatusCode();

        NetherNetPeer peer = new(clientIdentity: _clientIdentity);
        try {
            string offer = await peer.CreateOfferAsync(cancellationToken).ConfigureAwait(false);
            Uri endpoint = new(
                signalingEndpoint.AbsoluteUri + $"/{Uri.EscapeDataString(networkId)}");
            using StringContent content = new(offer, Encoding.UTF8, "application/sdp");
            using HttpResponseMessage response = await _httpClient
                .PostAsync(endpoint, content, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType is not MediaTypeHeaderValue contentType ||
                contentType.MediaType?.Equals("application/sdp", StringComparison.OrdinalIgnoreCase) != true) {
                throw new InvalidDataException("The NetherNet signaling response was not SDP.");
            }

            string answer = await response.Content.ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
            peer.AcceptAnswer(answer);
            await peer.WaitForChannelsAsync(cancellationToken).ConfigureAwait(false);
            return peer;
        }
        catch {
            peer.Dispose();
            throw;
        }
    }
}
