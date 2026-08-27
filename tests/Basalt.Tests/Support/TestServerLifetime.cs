namespace Basalt.Tests.Support;

using Basalt.Core;

internal sealed class TestServerLifetime : IDisposable {
    public TestServerLifetime(Properties properties) {
        Server = new Server(properties);
    }

    public Server Server { get; }

    public void Dispose() {
        Server.Stop();
    }
}
