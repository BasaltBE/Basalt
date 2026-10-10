using System.Threading.Channels;
using BasaltBE.Logging;
using BasaltBE.NetherNet;
using BasaltBE.NetherNet.Enums;
using BasaltBE.NetherNet.Types;

namespace BasaltBE.Basalt;

public sealed class Server : IAsyncDisposable {
    private static readonly ScopedLogger Log = Logger.Scoped("Server");
    private readonly NethernetServer nethernet;
    private int running;

    public Server(NethernetServerOptions options, IServerIdentity? identity = null) {
        nethernet = new NethernetServer(options, identity);
    }

    public ulong NetworkId => nethernet.NetworkId;

    public async Task RunAsync(Func<NethernetMessage, ValueTask> receive, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(receive);
        if (Interlocked.Exchange(ref running, 1) != 0)
            throw new InvalidOperationException("Server has already started.");

        try {
            await nethernet.StartAsync(cancellationToken).ConfigureAwait(false);
            Log.Info($"Server is listening on NetherNet network {NetworkId}.");

            while (!cancellationToken.IsCancellationRequested) {
                NethernetMessage message = await nethernet.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                await receive(message).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (ChannelClosedException) { }
        finally {
            await nethernet.DisposeAsync().ConfigureAwait(false);
        }
    }

    public ValueTask SendPacketAsync(
        NethernetPeer peer,
        ReadOnlyMemory<byte> packet,
        MessageReliability reliability = MessageReliability.Reliable,
        CancellationToken cancellationToken = default
    ) => nethernet.SendAsync(peer, packet, reliability, cancellationToken);

    public ValueTask DisposeAsync() => nethernet.DisposeAsync();
}
