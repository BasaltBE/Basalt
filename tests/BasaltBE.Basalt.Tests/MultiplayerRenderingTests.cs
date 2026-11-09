using BasaltBE.Basalt.Entities;
using BasaltBE.Basalt.Entities.Traits;
using BasaltBE.Basalt.Entities.Types;
using BasaltBE.Protocol.Packets;
using Xunit;

namespace BasaltBE.Basalt.Tests;

public sealed class MultiplayerRenderingTests {
    [Fact]
    public void PlayerMustBeListedAndItsChunkLoadedBeforeAddPlayer() {
        using MultiplayerFixture fixture = new();
        Player viewer = fixture.CreatePlayer();
        Player other = fixture.CreatePlayer();
        PlayerChunkRenderingTrait trait = new(viewer);
        List<DataPacket> sent = [];
        trait.UpdateEntities([viewer, other], sent.Add);
        Assert.Empty(sent);
        MultiplayerFixture.LoadedChunks(trait).Add(0);
        other.Listed = false;
        trait.UpdateEntities([viewer, other], sent.Add);
        Assert.Empty(sent);
        other.Listed = true;
        trait.UpdateEntities([viewer, other], sent.Add);
        Assert.Equal(3, sent.Count);
        AddPlayerPacket add = Assert.IsType<AddPlayerPacket>(sent[0]);
        Assert.Equal(other.UniqueId, Assert.IsType<UpdateAbilitiesPacket>(sent[1]).Data.TargetPlayerRawId);
        Assert.Equal(other.RuntimeId, Assert.IsType<SetActorDataPacket>(sent[2]).ActorRuntimeId);
        Assert.Equal(other.RuntimeId, add.RuntimeId);
        Assert.Equal(other.UniqueId, add.Abilities.TargetPlayerRawId);
        Assert.Equal(other.Position.Y, add.Position.Y);
        Assert.True(trait.Viewing(other));
        sent.Clear();
        trait.UpdateEntities([viewer, other], sent.Add);
        Assert.Empty(sent);
    }

    [Fact]
    public void LeavingChunkAndReenteringRemovesThenAddsPlayer() {
        using MultiplayerFixture fixture = new();
        Player other = fixture.CreatePlayer();
        PlayerChunkRenderingTrait trait = new(fixture.CreatePlayer());
        MultiplayerFixture.LoadedChunks(trait).Add(0);
        List<DataPacket> sent = [];
        trait.UpdateEntities([other], sent.Add);
        sent.Clear();
        other.Position.X = -0.01f;
        trait.UpdateEntities([other], sent.Add);
        Assert.Equal(other.UniqueId, Assert.IsType<RemoveActorPacket>(Assert.Single(sent)).ActorId);
        Assert.False(trait.Viewing(other));
        sent.Clear();
        other.Position.X = 0;
        trait.UpdateEntities([other], sent.Add);
        Assert.Equal(3, sent.Count);
        Assert.IsType<AddPlayerPacket>(sent[0]);
    }

    [Fact]
    public void MembershipRemovalAndDisposedPlayersDisappear() {
        using MultiplayerFixture fixture = new();
        Player other = fixture.CreatePlayer();
        PlayerChunkRenderingTrait trait = new(fixture.CreatePlayer());
        MultiplayerFixture.LoadedChunks(trait).Add(0);
        List<DataPacket> sent = [];
        trait.UpdateEntities([other], sent.Add);
        sent.Clear();
        other.Dispose();
        trait.UpdateEntities([other], sent.Add);
        Assert.IsType<RemoveActorPacket>(Assert.Single(sent));
        sent.Clear();
        trait.UpdateEntities([], sent.Add);
        Assert.Empty(sent);
    }

    [Fact]
    public void ActorsSpawnAndDespawnWithoutIdlePackets() {
        using MultiplayerFixture fixture = new();
        PlayerChunkRenderingTrait trait = new(fixture.CreatePlayer());
        MultiplayerFixture.LoadedChunks(trait).Add(0);
        Entity actor = new(new EntityType { Identifier = "minecraft:pig" });
        List<DataPacket> sent = [];
        trait.UpdateEntities([actor], sent.Add);
        Assert.IsType<AddActorPacket>(Assert.Single(sent));
        sent.Clear();
        trait.UpdateEntities([actor], sent.Add);
        Assert.Empty(sent);
        trait.UpdateEntities([], sent.Add);
        Assert.Equal(actor.UniqueId, Assert.IsType<RemoveActorPacket>(Assert.Single(sent)).ActorId);
    }

    [Fact]
    public void RenderDoesNotClearAnotherPlayersDirtyState() {
        using MultiplayerFixture fixture = new();
        Player other = fixture.CreatePlayer();
        other.Abilities.Dirty = true;
        other.Flags.Dirty = true;
        PlayerChunkRenderingTrait trait = new(fixture.CreatePlayer());
        MultiplayerFixture.LoadedChunks(trait).Add(0);
        trait.UpdateEntities([other], _ => { });
        Assert.True(other.Abilities.Dirty);
        Assert.True(other.Flags.Dirty);
    }

    [Fact]
    public void MembershipSnapshotsAreCachedAndInvalidated() {
        using MultiplayerFixture fixture = new();
        Entity actor = new();
        fixture.Dimension.AddEntity(actor);
        Assert.Equal(1, fixture.Dimension.GetEntities().Length);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 1000; index++)
            _ = fixture.Dimension.GetEntities();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, allocated);
        Assert.True(fixture.Dimension.RemoveEntity(actor));
        Assert.Empty(fixture.Dimension.GetEntities().ToArray());
    }

    [Fact]
    public void DimensionSnapshotsDoNotIncludeOtherDimensions() {
        using MultiplayerFixture fixture = new();
        var other = fixture.World.CreateDimension("minecraft:nether");
        Entity actor = new(new EntityType { Identifier = "minecraft:pig" });
        other.AddEntity(actor);
        Assert.Empty(fixture.Dimension.GetEntities().ToArray());
        Assert.Same(actor, Assert.Single(other.GetEntities().ToArray()));
    }

    [Fact]
    public void SpatialIndexTracksNegativeCoordinatesMovementAndRemoval() {
        using MultiplayerFixture fixture = new();
        Entity actor = new();
        actor.Position.X = -0.01f;
        fixture.Dimension.AddEntity(actor);
        fixture.World.Tick();
        List<Entity> candidates = [];
        HashSet<long> loaded = [unchecked((long)0xFFFFFFFF00000000)];
        fixture.Dimension.EntityIndex.Collect(loaded, candidates);
        Assert.Same(actor, Assert.Single(candidates));
        actor.Position.X = 0;
        actor.OnMove();
        fixture.World.Tick();
        fixture.Dimension.EntityIndex.Collect(loaded, candidates);
        Assert.Empty(candidates);
        loaded.Clear();
        loaded.Add(0);
        fixture.Dimension.EntityIndex.Collect(loaded, candidates);
        Assert.Same(actor, Assert.Single(candidates));
        fixture.Dimension.RemoveEntity(actor);
        fixture.World.Tick();
        fixture.Dimension.EntityIndex.Collect(loaded, candidates);
        Assert.Empty(candidates);
    }

    [Fact]
    public void UnloadedChunksRemoveVisibleEntitiesWithoutNewMovement() {
        using MultiplayerFixture fixture = new();
        PlayerChunkRenderingTrait trait = new(fixture.CreatePlayer());
        HashSet<long> loaded = MultiplayerFixture.LoadedChunks(trait);
        loaded.Add(0);
        Entity actor = new(new EntityType { Identifier = "minecraft:pig" });
        List<DataPacket> sent = [];
        trait.UpdateEntities([actor], sent.Add);
        sent.Clear();
        loaded.Clear();
        trait.UpdateEntities([actor], sent.Add);
        Assert.IsType<RemoveActorPacket>(Assert.Single(sent));
        Assert.False(trait.Viewing(actor));
    }

    [Fact]
    public void ParallelEntityCreationSharesTheTraitCacheSafely() {
        EntityType type = new() { Identifier = "minecraft:pig" };
        Parallel.For(0, 1000, _ => new Entity(type));
    }
}
