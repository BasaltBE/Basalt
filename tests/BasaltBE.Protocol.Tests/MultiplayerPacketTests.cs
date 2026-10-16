using Basalt.Binary;
using BasaltBE.Protocol.Enums;
using BasaltBE.Protocol.Packets;
using BasaltBE.Protocol.Types;
using BinaryReader = Basalt.Binary.BinaryReader;
using BinaryWriter = Basalt.Binary.BinaryWriter;
using Xunit;

namespace BasaltBE.Protocol.Tests;

public sealed class MultiplayerPacketTests
{
    [Fact]
    public void PlayerListRemoveMatchesReferenceWireBytes()
    {
        PlayerListPacket packet = new()
        {
            Entries = [new PlayerListEntry
            {
                Action = PlayerListAction.Remove,
                RemoveEntry = new PlayerListRemoveEntry
                {
                    Uuid = new Uuid { MostSignificantBits = 1, LeastSignificantBits = 2 }
                }
            }]
        };
        using BinaryStream stream = BinaryStream.Rent(64);
        BinaryWriter writer = stream.GetWriter();
        packet.Serialize(ref writer);
        Assert.Equal(Convert.FromHexString("01000101000000000000000200000000000000"),
            stream.Buffer.Span[..stream.Offset].ToArray());
    }

    [Theory]
    [InlineData(64, 64)]
    [InlineData(128, 128)]
    public void CachedAndTypedSkinsProduceIdenticalPlayerLists(int width, int height)
    {
        SerializedSkin skin = new()
        {
            Id = "test-skin",
            FullId = "test-skin",
            ResourcePatch = "{\"geometry\":{\"default\":\"geometry.humanoid.custom\"}}",
            GeometryData = "{}",
            ImageData = new SkinImage { Width = (uint)width, Height = (uint)height, ImageBytes = new byte[width * height * 4] },
            IsPrimaryUser = true,
            TrustedSkinFlag = TrustedSkinFlag.Unset
        };
        using BinaryStream skinStream = BinaryStream.Rent(128 * 1024);
        BinaryWriter skinWriter = skinStream.GetWriter();
        skin.Write(ref skinWriter);
        PlayerListAddEntry add = new() { ActorId = 123, Name = "Player", Xuid = "1234", Skin = skin, IsHost = true, PlayerColor = 123 };
        PlayerListPacket packet = new()
        {
            Entries = [new PlayerListEntry { Action = PlayerListAction.Add, AddEntry = add }]
        };
        using BinaryStream typed = BinaryStream.Rent(128 * 1024);
        BinaryWriter typedWriter = typed.GetWriter();
        packet.Serialize(ref typedWriter);
        Assert.Equal(new byte[] { 1, 1, 0 }, typed.Buffer.Span[..3].ToArray());

        add.SkinBytes = skinStream.Buffer[..skinStream.Offset];
        using BinaryStream cached = BinaryStream.Rent(128 * 1024);
        BinaryWriter cachedWriter = cached.GetWriter();
        packet.Serialize(ref cachedWriter);
        Assert.Equal(typed.Buffer.Span[..typed.Offset].ToArray(), cached.Buffer.Span[..cached.Offset].ToArray());

        cached.Offset = 0;
        BinaryReader reader = cached.GetReader();
        PlayerListPacket decoded = new();
        decoded.Deserialize(ref reader);
        PlayerListAddEntry result = Assert.Single(decoded.Entries).AddEntry!;
        Assert.Equal("test-skin", result.Skin.Id);
        Assert.Equal(skin.ImageData.ImageBytes, result.Skin.ImageData.ImageBytes);
        Assert.Equal(add.ActorId, result.ActorId);
        Assert.Equal(123, result.PlayerColor);
        Assert.True(result.IsHost);
        Assert.Equal(typed.Offset, cached.Offset);
    }

    [Fact]
    public void RawSkinSpanDoesNotAddAByteArrayLength()
    {
        PlayerListAddEntry entry = new();
        using BinaryStream stream = BinaryStream.Rent(256);
        BinaryWriter writer = stream.GetWriter();
        entry.Write(ref writer, new byte[] { 0xAB, 0xCD });
        Assert.Equal(new byte[] { 0xAB, 0xCD }, stream.Buffer.Span.Slice(25, 2).ToArray());
    }

    [Fact]
    public void PlayerListRejectsOversizedCountBeforeAllocation()
    {
        Assert.Throws<InvalidDataException>(() =>
        {
            byte[] data = [0xE9, 0x07];
            int offset = 0;
            BinaryReader reader = new(data, ref offset);
            new PlayerListPacket().Deserialize(ref reader);
        });
    }

    [Fact]
    public void PlayerListRejectsVariantActionMismatch()
    {
        Assert.Throws<InvalidDataException>(() =>
        {
            byte[] data = [1, 0, 0];
            int offset = 0;
            BinaryReader reader = new(data, ref offset);
            new PlayerListPacket().Deserialize(ref reader);
        });
    }

    [Fact]
    public void MixedPlayerListEntriesRoundTrip()
    {
        PlayerListPacket packet = new()
        {
            Entries = [
                new PlayerListEntry { Action = PlayerListAction.Add, AddEntry = new PlayerListAddEntry { Name = "One" } },
                new PlayerListEntry { Action = PlayerListAction.Remove, RemoveEntry = new PlayerListRemoveEntry() }
            ]
        };
        using BinaryStream stream = BinaryStream.Rent(1024);
        BinaryWriter writer = stream.GetWriter();
        packet.Serialize(ref writer);
        int length = stream.Offset;
        stream.Offset = 0;
        BinaryReader reader = stream.GetReader();
        PlayerListPacket decoded = new();
        decoded.Deserialize(ref reader);
        Assert.Equal(2, decoded.Entries.Length);
        Assert.Equal("One", decoded.Entries[0].AddEntry!.Name);
        Assert.Equal(PlayerListAction.Remove, decoded.Entries[1].Action);
        Assert.Equal(length, stream.Offset);
    }

    [Fact]
    public void AddPlayerRoundTripsIdentityPositionAbilitiesAndMetadata()
    {
        AddPlayerPacket packet = new()
        {
            Uuid = new Uuid { MostSignificantBits = 123, LeastSignificantBits = 456 },
            Name = "Other",
            RuntimeId = 123456,
            Position = new Vec3 { X = -16.5f, Y = -58.38f, Z = 32.5f },
            Rotation = new Vec2 { X = 15, Y = 120 },
            HeadRotation = 110,
            GameType = GameType.Creative,
            Abilities = new SerializedAbilitiesData { TargetPlayerRawId = -42, Layers = [new SerializedAbilitiesLayer { Layer = 1, WalkSpeed = 0.1f }] },
            ActorData = [new ActorDataItem { Id = 0, Type = DataItemType.Int64, Value = 123L }],
            DeviceId = "device",
            BuildPlatform = BuildPlatform.Win32
        };
        using BinaryStream stream = BinaryStream.Rent(2048);
        BinaryWriter writer = stream.GetWriter();
        packet.Serialize(ref writer);
        int length = stream.Offset;
        stream.Offset = 0;
        BinaryReader reader = stream.GetReader();
        AddPlayerPacket decoded = new();
        decoded.Deserialize(ref reader);
        Assert.Equal(packet.Uuid.MostSignificantBits, decoded.Uuid.MostSignificantBits);
        Assert.Equal(packet.RuntimeId, decoded.RuntimeId);
        Assert.Equal(packet.Position.Y, decoded.Position.Y);
        Assert.Equal(packet.Rotation.Y, decoded.Rotation.Y);
        Assert.Equal(packet.GameType, decoded.GameType);
        Assert.Equal(-42, decoded.Abilities.TargetPlayerRawId);
        Assert.Equal(123L, Assert.Single(decoded.ActorData).Value);
        Assert.Equal("device", decoded.DeviceId);
        Assert.Equal(length, stream.Offset);
    }

    [Fact]
    public void AddPlayerGameTypeUsesSignedVarInt()
    {
        AddPlayerPacket packet = new() { GameType = GameType.Creative };
        using BinaryStream stream = BinaryStream.Rent(256);
        BinaryWriter writer = stream.GetWriter();
        packet.Serialize(ref writer);
        Assert.Equal(2, stream.Buffer.Span[63]);
    }
}
