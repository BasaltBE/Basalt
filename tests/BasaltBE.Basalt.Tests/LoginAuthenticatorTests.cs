using System.Collections.Concurrent;
using System.Diagnostics;
using BasaltBE.Basalt.Network.Login;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class LoginAuthenticatorTests {
    [Fact]
    public void FetchesSigningKeysOnBackgroundThreadAndUsesCachedKeys() {
        int callerThread = Environment.CurrentManagedThreadId;
        ConcurrentQueue<(int ThreadId, bool Background)> fetchThreads = new();
        int fetchCount = 0;
        using LoginAuthenticator authenticator = new(token => {
            fetchThreads.Enqueue((Environment.CurrentManagedThreadId, Thread.CurrentThread.IsBackground));
            Interlocked.Increment(ref fetchCount);
            return [new OpenIdSigningKey { KeyId = "key-1", Algorithm = "RS256", KeyType = "RSA" }];
        }, TimeSpan.FromHours(1));

        authenticator.Start();

        Assert.True(fetchThreads.TryPeek(out var fetchThread));
        Assert.NotEqual(callerThread, fetchThread.ThreadId);
        Assert.True(fetchThread.Background);
        Assert.Equal("key-1", authenticator.GetSigningKey("key-1", "RS256").KeyId);
        Assert.Equal(1, Volatile.Read(ref fetchCount));
    }

    [Fact]
    public void CachedKeyLookupDoesNotWaitForBackgroundRefresh() {
        using ManualResetEventSlim refreshEntered = new(false);
        using ManualResetEventSlim releaseRefresh = new(false);
        int fetchCount = 0;
        using LoginAuthenticator authenticator = new(token => {
            if (Interlocked.Increment(ref fetchCount) > 1) {
                refreshEntered.Set();
                releaseRefresh.Wait(token);
            }
            return [new OpenIdSigningKey { KeyId = "key-1", Algorithm = "RS256", KeyType = "RSA" }];
        }, TimeSpan.FromMilliseconds(10));

        try {
            authenticator.Start();
            Assert.True(refreshEntered.Wait(TimeSpan.FromSeconds(2)));

            Stopwatch stopwatch = Stopwatch.StartNew();
            Assert.Equal("key-1", authenticator.GetSigningKey("key-1", "RS256").KeyId);
            stopwatch.Stop();

            Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(100));
        }
        finally {
            releaseRefresh.Set();
        }
    }
}
