namespace Basalt.Tests;

using Basalt.Core.Entities.AI.Memory;

public sealed class MemoryStoreTests {
    [Fact]
    public void RegisteredMemoryStartsEmpty() {
        MemoryStore store = new();
        MemoryKey key = new("test");

        store.Register(key);

        Assert.Equal(MemoryStatus.Registered, store.GetStatus(key));
        Assert.False(store.Has(key));
    }

    [Fact]
    public void SettingMemoryMakesItPresent() {
        MemoryStore store = new();
        MemoryKey key = new("test");

        store.Set(key, 42);

        Assert.Equal(MemoryStatus.Present, store.GetStatus(key));
        Assert.True(store.TryRead<int>(key, out int value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void ExpiredMemoryIsRemoved() {
        MemoryStore store = new();
        MemoryKey key = new("test");
        store.Set(key, "value", 10);

        store.Expire(10);

        Assert.Equal(MemoryStatus.Registered, store.GetStatus(key));
        Assert.False(store.TryRead<string>(key, out _));
    }

    [Fact]
    public void RemovingMemoryKeepsRegistration() {
        MemoryStore store = new();
        MemoryKey key = new("test");
        store.Set(key, "value");

        Assert.True(store.Remove(key));
        Assert.Equal(MemoryStatus.Registered, store.GetStatus(key));
    }
}
